using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API
{
    public class WebsocketReference
    {
        private readonly API.APIWebSocketProtocol m_Client;
        private readonly Path m_Path;
        private readonly string m_ClientID;

        public Path Path => m_Path;
        public string ClientID => m_ClientID;

        internal WebsocketReference(API.APIWebSocketProtocol client, Path path)
        {
            m_Client = client;
            m_Path = path;
            m_ClientID = Guid.NewGuid().ToString();
        }

        public async Task Disconnect() => await m_Client.Disconnect();
        public async Task Send(object msg)
        {
            string? message = msg.ToString();
            if (!string.IsNullOrEmpty(message))
                await m_Client.SendToClient(message);
        }

        public void Reconnect() { /*TODO*/ }
    }
}
