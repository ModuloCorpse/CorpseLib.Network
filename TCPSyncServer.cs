using static CorpseLib.Network.ATCPServer;

namespace CorpseLib.Network
{
    public class TCPSyncServer(ProtocolFactory protocolFactory, int port) : ATCPServer(protocolFactory, port)
    {
        public TCPSyncClient Accept()
        {
            TCPSyncClient client = new(NewProtocol(), NewClientID(), m_ServerSocket.Accept());
            AddClient(client);
            return client;
        }
    }
}
