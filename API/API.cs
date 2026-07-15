using CorpseLib.Logging;
using CorpseLib.Network.Http;
using System.Net;
using System.Net.WebSockets;
using static CorpseLib.Network.Http.ResourceSystem;
using Directory = CorpseLib.Network.Http.ResourceSystem.Directory;

namespace CorpseLib.Network.API
{
    public class API
    {
        public static readonly Logger API_DEBUGGER = new("[${d}-${M}-${y} ${h}:${m}:${s}.${ms}] ${log}");
        public static void StartLogging() => API_DEBUGGER.Start();
        public static void StopLogging() => API_DEBUGGER.Stop();

        internal class APIWebSocketProtocol(API server, AWebsocketEndpoint endpoint, Http.Path path) : WebSocket.AWebSocketProtocol
        {
            private readonly AWebsocketEndpoint m_Endpoint = endpoint;
            private readonly Http.Path m_Path = path;
            private WebsocketReference? m_Reference = null;

            internal void SendToClient(string message) => Send(message);

            public override void OnOpen()
            {
                m_Reference = new(this, m_Path);
                m_Endpoint.RegisterClient(m_Reference);
                server.Register(m_Reference.ClientID, this);
            }

            public override void OnClose(int status, string message)
            {
                m_Endpoint.ClientUnregistered(m_Reference!);
                server.Unregister(m_Reference!.ClientID);
            }

            public override void HandleMessage(string message)
            {
                m_Endpoint.ClientMessage(m_Reference!, message);
            }

            public override void OnError(Exception ex) { }
        }

        private void Register(string clientID, APIWebSocketProtocol webServerWebSocketProtocol)
        {
            m_Lock.Enter();
            m_OpenWebSockets.Add(clientID, webServerWebSocketProtocol);
            m_Lock.Exit();
        }

        private void Unregister(string clientID)
        {
            m_Lock.Enter();
            m_OpenWebSockets.Remove(clientID);
            m_Lock.Exit();
        }

        private readonly Dictionary<string, APIWebSocketProtocol> m_OpenWebSockets = [];
        private readonly ResourceSystem m_ResourceSystem = new();
        private readonly HttpListener m_Listener = new();
        private readonly Lock m_Lock = new();
        private readonly int m_Port;
        private bool m_IsRunning = false;

        public int Port => m_Port;
        public bool IsRunning => m_IsRunning;

        public API(int port)
        {
            m_Port = port;
            m_Listener.Prefixes.Add($"http://localhost:{m_Port}/");
        }

        public void AddEndpoint(string path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler) => AddEndpoint(new Http.Path(path), methodType, methodHandler);

        public void AddEndpoint(Http.Path path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler)
        {
            Resource? endpoint = m_ResourceSystem.Get(path);
            if (endpoint != null && endpoint is HTTPEndpoint httpEndpoint)
                httpEndpoint.SetEndpoint(methodType, methodHandler);
            else
            {
                HTTPEndpoint newEndpoint = new(path, true);
                newEndpoint.SetEndpoint(methodType, methodHandler);
                m_ResourceSystem.Add(path, newEndpoint);
            }
        }

        public void AddDirectory(Http.Path path, ResourceSystem.Directory directory) => m_ResourceSystem.Add(path, directory);
        public void AddEndpoint(AEndpoint endpoint)
        {
            m_ResourceSystem.Add(endpoint.Path, endpoint);
            endpoint.SetAPI(this);
        }

        public Resource? GetResource(Http.Path path) => m_ResourceSystem.Get(path);

        public void Start()
        {
            try
            {
                m_Listener.Start();
            } catch (Exception ex)
            {
                API_DEBUGGER.Log($"Cannot start server: {ex.Message}");
                return;
            }
            API_DEBUGGER.Log($"Server started on port {m_Port}");
            m_IsRunning = true;
            Thread serverThread = new(HandleRequest) { IsBackground = true };
            serverThread.Start();
        }

        public void Stop()
        {
            m_Lock.Enter();
            foreach (APIWebSocketProtocol protocol in m_OpenWebSockets.Values)
                protocol.Disconnect();
            m_OpenWebSockets.Clear();
            m_Lock.Exit();

            m_IsRunning = false;
            m_Listener.Stop();
        }

        private async Task ProcessContext(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest httpRequest = context.Request;
                HttpListenerResponse httpResponse = context.Response;

                Request request = new(httpRequest);
                Response? response = null;

                API_DEBUGGER.Log("Received : ${0}", request);
                Resource? resource = m_ResourceSystem.Get(request.Path);
                if (resource == null)
                    response = new(404, "Not Found");
                else
                {
                    if (resource is Directory directory)
                    {
                        resource = directory.Get(new());
                        if (resource == null)
                            response = new(404, "Not Found");
                    }

                    if (httpRequest.IsWebSocketRequest)
                    {
                        AWebsocketEndpoint? websocketEndpoint = null;
                        if (resource is AWebsocketEndpoint wsEndpoint)
                            websocketEndpoint = wsEndpoint;
                        else if (resource is WebEndpoints webEndpoints)
                            websocketEndpoint = webEndpoints.WebsocketEndpoint;

                        if (websocketEndpoint != null)
                        {
                            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(subProtocol: null);

                            APIWebSocketProtocol protocol = new(this, websocketEndpoint, request.Path);
                            _ = WebSocket.WebSocketClient.Connect(wsContext.WebSocket, protocol);
                            API_DEBUGGER.Log($"Websocket connection established");
                            return;
                        }
                        else
                            response = new(403, "Forbidden");
                    }
                    else
                    {
                        AHTTPEndpoint? httpEndpoint = null;
                        if (resource is AHTTPEndpoint resourceEndpoint)
                            httpEndpoint = resourceEndpoint;
                        else if (resource is WebEndpoints webEndpoints)
                            httpEndpoint = webEndpoints.HTTPEndpoint;

                        if (httpEndpoint != null)
                            response = httpEndpoint.HandleRequest(request);
                        else
                            response = new(403, "Forbidden");
                    }
                }

                API_DEBUGGER.Log("Sending : ${0}", response);
                response?.ToHttpResponseMessage(httpResponse);
                httpResponse.Close();
            }
            catch (Exception ex)
            {
                if (m_IsRunning)
                    API_DEBUGGER.Log($"Server error: {ex.Message}");
            }
        }

        private void HandleRequest()
        {
            while (m_IsRunning)
            {
                try
                {
                    HttpListenerContext context = m_Listener.GetContext();
                    Task.Run(async () => await ProcessContext(context));
                }
                catch (Exception ex)
                {
                    if (m_IsRunning)
                        API_DEBUGGER.Log($"Server error: {ex.Message}");
                }
            }
        }

        public List<KeyValuePair<Http.Path, Resource>> FlattenEndpoints() => m_ResourceSystem.Flatten();
    }
}
