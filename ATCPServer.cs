using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace CorpseLib.Network
{
    /// <summary>
    /// Class representing an asynchronous TCP server
    /// </summary>
    /// <remarks>
    /// Create a TCP server on the specified <paramref name="port"/>
    /// </remarks>
    /// <param name="port">Port on which the server will listen</param>
    public abstract class ATCPServer(ATCPServer.ProtocolFactory protocolFactory, int port)
    {
        public delegate AProtocol ProtocolFactory();

        private readonly ProtocolFactory m_ProtocolFactory = protocolFactory;
        private readonly ConcurrentDictionary<int, ATCPClient> m_Clients = new();
        private readonly List<int> m_FreeIdx = [];
        protected readonly Socket m_ServerSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly MonitorBatch m_Monitor = [];
        private int m_CurrentIdx = 0;
        private readonly int m_Port = port;
        private volatile bool m_Running = false;

        public void AddMonitor(IMonitor monitor) => m_Monitor.Add(monitor);

        public bool IsRunning() => m_Running;

        protected int NewClientID()
        {
            int clientID = m_CurrentIdx;
            if (m_FreeIdx.Count == 0)
                ++m_CurrentIdx;
            else
            {
                clientID = m_FreeIdx[0];
                m_FreeIdx.RemoveAt(0);
            }
            return clientID;
        }

        protected AProtocol NewProtocol() => m_ProtocolFactory();

        protected void AddClient(ATCPClient client)
        {
            if (!m_Running)
                return;
            if (m_Monitor != null)
                client.AddMonitor(m_Monitor);
            client.OnDisconnect += delegate (ATCPClient client)
            {
                int clientID = client.GetID();
                m_Clients.Remove(clientID, out var _);
                m_FreeIdx.Add(clientID);
            };
            m_Clients[client.GetID()] = client;
        }

        public void Listen()
        {
            if (!m_Running)
            {
                m_Running = true;
                try
                {
                    m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, m_Port));
                    m_ServerSocket.Listen(10);
                }
                catch (Exception ex)
                {
                    Stop();
                    throw new Exception("Listening error" + ex);
                }
            }
        }

        public void Stop()
        {
            if (m_Running)
            {
                m_Running = false;
                foreach (var pair in m_Clients)
                    pair.Value.Disconnect();
                m_Clients.Clear();
                m_ServerSocket.Close();
            }
        }

        ~ATCPServer() => Stop();
    }
}
