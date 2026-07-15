namespace CorpseLib.Network
{
    public interface IMonitor
    {
        void OnOpening();
        void OnOpen();
        void OnReopening();
        void OnReopen();
        void OnSend(object obj);
        void OnSend(byte[] bytes);
        void OnReceive(byte[] bytes);
        void OnReceive(object obj);
        void OnClose();
        void OnException(Exception ex);
        void OnLog(string log);
    }
}
