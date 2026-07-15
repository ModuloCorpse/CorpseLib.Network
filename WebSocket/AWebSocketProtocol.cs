namespace CorpseLib.Network.WebSocket
{
    public abstract class AWebSocketProtocol
    {
        private WebSocketClient? m_Socket;

        internal void SetSocket(WebSocketClient socket) => m_Socket = socket;

        protected void SetIsReadOnly(bool isReadOnly) => m_Socket?.SetIsReadOnly(isReadOnly);
        protected void Send(string message) => m_Socket?.Send(message);

        protected string SendAndWaitResponse(string message) => m_Socket?.SendAndWaitResponse(message) ?? string.Empty;

        public bool IsConnected() => m_Socket != null && m_Socket.IsConnected();
        public void Disconnect() => m_Socket?.Disconnect();
        public void Reconnect() => m_Socket?.Reconnect();

        public abstract void HandleMessage(string message);
        public abstract void OnOpen();
        public abstract void OnClose(int status, string message);
        public abstract void OnError(Exception ex);
    }
}
