using CorpseLib.Logging;
using System.Text;

namespace CorpseLib.Network
{
    public class FullDebugLogMonitor(Logger logger) : IMonitor
    {
        private readonly Logger m_Logger = logger;

        public void OnClose() => m_Logger.Log("[MONITOR] Connection closed");
        public void OnOpening() => m_Logger.Log("[MONITOR] Connection opening");
        public void OnOpen() => m_Logger.Log("[MONITOR] Connection open");
        public void OnReopening() => m_Logger.Log("[MONITOR] Connection reopenning");
        public void OnReopen() => m_Logger.Log("[MONITOR] Connection reopenned");
        public void OnReceive(byte[] bytes) => m_Logger.Log("[MONITOR] Received ${0} bytes [UTF-8 : ${1}]", bytes.Length, Encoding.UTF8.GetString(bytes));
        public void OnReceive(object obj) => m_Logger.Log("[MONITOR] Received: ${0}", obj);
        public void OnSend(object obj) => m_Logger.Log("[MONITOR] Sent: ${0}", obj);
        public void OnSend(byte[] bytes) => m_Logger.Log("[MONITOR] Sent ${0} bytes [UTF-8 : ${1}]", bytes.Length, Encoding.UTF8.GetString(bytes));
        public void OnException(Exception ex) => m_Logger.Log("[MONITOR] Exception: ${0}", ex);
        public void OnLog(string log) => m_Logger.Log("[MONITOR] Log: ${0}", log);
    }
}
