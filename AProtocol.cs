using CorpseLib.Serialize;

namespace CorpseLib.Network
{
    public abstract class AProtocol
    {
        private ATCPClient? m_Client = null;

        //====================PROTECTED====================\\
        protected List<object> ReadFromStream() => m_Client!.Read();
        protected void SetStream(Stream stream) => m_Client!.SetStream(stream);
        protected Stream GetStream() => m_Client!.GetStream();
        protected abstract void OnClientConnected();
        protected abstract void OnClientReset();
        protected abstract void OnClientReconnected();
        protected abstract void OnClientDisconnected();
        protected abstract OperationResult<object> Read(BytesReader reader);
        protected virtual void Write(BytesWriter writer, object obj) => writer.Write(obj);
        protected abstract void Treat(object packet);
        protected abstract void SetupSerializer(ref BytesSerializer serializer);
        protected virtual void OnDiscardException(Exception exception) { }
        protected void SetReadOnly(bool isReadOnly) => m_Client!.SetReadOnly(isReadOnly);

        //====================INTERNAL====================\\
        internal void SetClient(ATCPClient client) => m_Client = client;
        internal OperationResult<object> ReadFromStream(BytesReader reader) => Read(reader);
        internal void TreatPacket(object packet) => Treat(packet);
        internal void ClientConnected() => OnClientConnected();
        internal void ClientReset() => OnClientReset();
        internal void ClientReconnected() => OnClientReconnected();
        internal void ClientDisconnected() => OnClientDisconnected();
        internal void FillSerializer(ref BytesSerializer serializer) => SetupSerializer(ref serializer);
        internal void DiscardException(Exception exception) => OnDiscardException(exception);

        internal void InternalSend(object msg)
        {
            BytesWriter writer = CreateBytesWriter();
            Write(writer, msg);
            m_Client!.WriteToStream(writer.Bytes);
        }

        //====================PUBLIC====================\\
        public bool IsSynchronous() => !m_Client!.IsAsynchronous();
        public bool IsAsynchronous() => m_Client!.IsAsynchronous();
        public URI GetURL() => m_Client!.GetURL();
        public int GetID() => m_Client!.GetID();
        public bool IsConnected() => m_Client!.IsConnected();
        public bool IsReconnecting() => m_Client!.IsReconnecting();
        public bool IsServerSide() => m_Client!.IsServerSide();
        public void Disconnect() => m_Client!.Disconnect();
        public void Connect() => m_Client!.Connect();
        public void Reconnect() => m_Client!.Reconnect();
        public void AddMonitor(IMonitor monitor) => m_Client!.AddMonitor(monitor);
        public void Send(object msg) => m_Client!.Send(msg);
        public void ForceSend(object msg) => m_Client!.ForceSend(msg);
        public BytesWriter CreateBytesWriter() => m_Client!.CreateBytesWriter();
        public void TestRead(BytesWriter bytesWriter) => m_Client!.TestRead(bytesWriter);
    }
}
