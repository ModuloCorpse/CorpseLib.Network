using System.Net.WebSockets;
using System.Text;

namespace CorpseLib.Network.WebSocket
{
    public class WebSocketClient(System.Net.WebSockets.WebSocket socket, CancellationTokenSource cancellationTokenSource, AWebSocketProtocol protocol, URI? uri) : IDisposable
    {
        private readonly Dictionary<string, string> m_Headers = [];
        private TaskCompletionSource<string>? m_TaskCompletionSource;
        private System.Net.WebSockets.WebSocket m_Socket = socket;
        private readonly CancellationTokenSource m_CancellationTokenSource = cancellationTokenSource;
        private readonly AWebSocketProtocol m_Protocol = protocol;
        private readonly URI? m_URI = uri;
        private bool m_IsReadOnly = false;

        internal void SetIsReadOnly(bool isReadOnly) => m_IsReadOnly = isReadOnly;
        internal bool IsConnected() => m_Socket.State == WebSocketState.Open;

        public static WebSocketClient? Connect(URI uri, AWebSocketProtocol protocol) => Connect(uri, protocol, []);
        public static WebSocketClient? Connect(URI uri, AWebSocketProtocol protocol, Dictionary<string, string> headers)
        {
            try
            {
                ClientWebSocket socket = new();
                CancellationTokenSource cancellationTokenSource = new();
                foreach (var header in headers)
                    socket.Options.SetRequestHeader(header.Key, header.Value);
                socket.ConnectAsync(new(uri.ToString()), cancellationTokenSource.Token).GetAwaiter().GetResult();
                WebSocketClient webSocket = new(socket, cancellationTokenSource, protocol, uri);
                foreach (var header in headers)
                    webSocket.m_Headers.Add(header.Key, header.Value);
                protocol.SetSocket(webSocket);
                protocol.OnOpen();
                Task.Run(() => webSocket.HandleReceive());
                return webSocket;
            }
            catch (Exception ex)
            {
                protocol.OnError(ex);
                return null;
            }
        }
        public static void Reconnect(WebSocketClient webSocket)
        {
            if (webSocket.m_URI == null)
                return;
            try
            {
                ClientWebSocket socket = new();
                CancellationTokenSource cancellationTokenSource = new();
                foreach (var header in webSocket.m_Headers)
                    socket.Options.SetRequestHeader(header.Key, header.Value);
                socket.ConnectAsync(new(webSocket.m_URI.ToString()), cancellationTokenSource.Token).GetAwaiter().GetResult();
                webSocket.m_Socket = socket;
                webSocket.m_Protocol.OnOpen();
                Task.Run(() => webSocket.HandleReceive());
            }
            catch (Exception ex)
            {
                webSocket.m_Protocol.OnError(ex);
            }
        }

        internal static WebSocketClient Connect(System.Net.WebSockets.WebSocket socket, AWebSocketProtocol protocol)
        {
            WebSocketClient webSocket = new(socket, new(), protocol, null);
            protocol.SetSocket(webSocket);
            protocol.OnOpen();
            Task.Run(() => webSocket.HandleReceive());
            return webSocket;
        }

        public void Disconnect()
        {
            if (m_Socket.State == WebSocketState.Open)
            {
                m_Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", m_CancellationTokenSource.Token).GetAwaiter().GetResult();
            }
        }

        public void Reconnect()
        {
            Disconnect();
            Reconnect(this);
        }

        private async Task HandleReceive()
        {
            var buffer = new byte[1024 * 4];

            while ((m_Socket.State == WebSocketState.Open || m_Socket.State == WebSocketState.CloseSent) && !m_CancellationTokenSource.IsCancellationRequested)
            {
                using var memoryStream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await m_Socket.ReceiveAsync(new ArraySegment<byte>(buffer), m_CancellationTokenSource.Token);
                    memoryStream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (m_Socket.State == WebSocketState.CloseReceived)
                        await m_Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge", m_CancellationTokenSource.Token);
                    m_Protocol.OnClose((int)result.CloseStatus!, result.CloseStatusDescription ?? string.Empty);
                    break;
                }
                else
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                    string message = await reader.ReadToEndAsync();

                    Console.WriteLine($"[{(m_URI == null ? "Server" : "Client")}] <= " + message);
                    if (m_TaskCompletionSource != null && !m_TaskCompletionSource.Task.IsCompleted)
                    {
                        m_TaskCompletionSource.SetResult(message);
                    }
                    else
                    {
                        m_Protocol.HandleMessage(message);
                    }
                }
            }
        }

        internal void Send(string message)
        {
            Console.WriteLine($"[{(m_URI == null ? "Server" : "Client")}] => " + message);
            if (m_IsReadOnly)
                return;
            if (m_Socket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                m_Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, m_CancellationTokenSource.Token).GetAwaiter().GetResult();
            }
        }

        internal string SendAndWaitResponse(string command)
        {
            if (m_IsReadOnly)
                return string.Empty;
            m_TaskCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Send(command);
            if (!m_TaskCompletionSource.Task.Wait(TimeSpan.FromSeconds(10)))
                return string.Empty;
            return m_TaskCompletionSource.Task.Result;
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
            m_Socket.Dispose();
            m_CancellationTokenSource.Dispose();
        }
    }
}
