namespace CorpseLib.Network.WebSocket
{
    public abstract class AWebSocketProtocol
    {
        private WebSocketClient? m_Socket;

        internal void SetSocket(WebSocketClient socket) => m_Socket = socket;

        protected void SetIsReadOnly(bool isReadOnly) => m_Socket?.SetIsReadOnly(isReadOnly);
        protected async Task Send(string message)
        {
            if (m_Socket != null)
                await m_Socket.Send(message);
        }

        protected async Task<string> SendAndWaitResponse(string message)
        {
            if (m_Socket != null)
                return await m_Socket.SendAndWaitResponse(message);
            return string.Empty;
        }

        public bool IsConnected() => m_Socket != null && m_Socket.IsConnected();

        public async Task Disconnect()
        {
            if (m_Socket != null)
                await m_Socket.Disconnect();
        }

        public async Task Reconnect()
        {
            if (m_Socket != null)
                await m_Socket.Reconnect();
        }

        public async Task HandleMessage(string message)
        {
            try
            {
                await OnMessageReceived(message);
            }
            catch (Exception ex)
            {
                await OnError(ex);
            }
        }

        public virtual async Task OnMessageReceived(string message) { }
        public virtual async Task OnOpen() { }
        public virtual async Task OnClose(int status, string message) { }
        public virtual async Task OnError(Exception ex) { }
    }
}
