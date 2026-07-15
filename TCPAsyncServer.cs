using System.Net.Sockets;
using static CorpseLib.Network.ATCPServer;

namespace CorpseLib.Network
{
    public class TCPAsyncServer(ProtocolFactory protocolFactory, int port) : ATCPServer(protocolFactory, port)
    {
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket acceptedSocket = m_ServerSocket.EndAccept(ar);
                TCPAsyncClient client = new(NewProtocol(), NewClientID(), acceptedSocket);
                AddClient(client);
                client.StartReceiving();
                m_ServerSocket.BeginAccept(AcceptCallback, null);
            }
            catch
            {
                Stop();
            }
        }

        public void Start()
        {
            Listen();
            if (IsRunning())
                m_ServerSocket.BeginAccept(AcceptCallback, null);
        }
    }
}
