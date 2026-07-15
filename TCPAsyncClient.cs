using CorpseLib.Serialize;
using System.Net.Sockets;

namespace CorpseLib.Network
{
    public class TCPAsyncClient : ATCPClient
    {
        private volatile bool m_IsReceiving = false;

        public TCPAsyncClient(AProtocol protocol, URI url, int id = 0) : base(protocol, url, id) { }
        internal TCPAsyncClient(AProtocol protocol, int id, Socket socket) : base(protocol, id, socket) { }

        private async void AsyncReceive()
        {
            byte[] readBuffer = new byte[1024];
            while (true)
            {
                byte[] buffer = [];
                try
                {
                    int bytesRead = await m_Stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length));
                    Append(ref buffer, readBuffer, bytesRead);
                    if (bytesRead == readBuffer.Length)
                        ReadAllStream(ref buffer);
                    if (buffer.Length > 0)
                        Received(buffer);
                }
                catch (Exception ex)
                {
                    DiscardException(ex);
                    UnwantedDisconnection();
                    return;
                }
            }
        }

        internal void StartReceiving()
        {
            m_IsReceiving = true;
            Task.Run(AsyncReceive);
        }

        protected override void HandleReceivedPacket(object packet) => m_Protocol.TreatPacket(packet);

        private async void ReconnectAsync()
        {
            uint tryCount = 0;
            var periodicTimer = new PeriodicTimer(Delay);
            while (await periodicTimer.WaitForNextTickAsync())
            {
                ++tryCount;
                if (InternalConnect(TimeSpan.FromMinutes(1), true))
                {
                    SetIsReconnecting(false);
                    if (m_IsReceiving)
                        StartReceiving();
                    return;
                }
                else if (tryCount >= MaxNbTry)
                {
                    SetIsReconnecting(false);
                    return;
                }
            }
        }

        protected override void HandleReconnect() => ReconnectAsync();

        public override void TestRead(BytesWriter bytesWriter) => TestReceived(bytesWriter);

        public override List<object> Read() => [];

        private async void ActionAfterReconnectAsync(Action action)
        {
            var periodicTimer = new PeriodicTimer(Delay);
            while (await periodicTimer.WaitForNextTickAsync())
            {
                if (!IsReconnecting())
                {
                    action();
                    return;
                }
            }
        }

        protected override void HandleActionAfterReconnect(Action action) => ActionAfterReconnectAsync(action);

        public bool Start()
        {
            if (Connect())
            {
                StartReceiving();
                return true;
            }
            return false;
        }

        public override bool IsAsynchronous() => true;
    }
}
