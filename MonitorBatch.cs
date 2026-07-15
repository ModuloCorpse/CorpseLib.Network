using System.Collections;

namespace CorpseLib.Network
{
    public class MonitorBatch() : IMonitor, IEnumerable<IMonitor>
    {
        private readonly List<IMonitor> m_Monitors = [];

        public void Add(IMonitor monitor)
        {
            if (monitor is MonitorBatch batch)
                m_Monitors.AddRange(batch.m_Monitors);
            else
                m_Monitors.Add(monitor);
        }

        public void OnOpening()
        {
            foreach (var monitor in m_Monitors)
                monitor.OnOpening();
        }

        public void OnOpen()
        {
            foreach (var monitor in m_Monitors)
                monitor.OnOpen();
        }

        public void OnReopening()
        {
            foreach (var monitor in m_Monitors)
                monitor.OnReopening();
        }

        public void OnReopen()
        {
            foreach (var monitor in m_Monitors)
                monitor.OnReopen();
        }

        public void OnSend(object obj)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnSend(obj);
        }

        public void OnSend(byte[] bytes)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnSend(bytes);
        }

        public void OnReceive(byte[] bytes)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnReceive(bytes);
        }

        public void OnReceive(object obj)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnReceive(obj);
        }

        public void OnClose()
        {
            foreach (var monitor in m_Monitors)
                monitor.OnClose();
        }

        public void OnException(Exception ex)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnException(ex);
        }

        public void OnLog(string log)
        {
            foreach (var monitor in m_Monitors)
                monitor.OnLog(log);
        }

        public IEnumerator<IMonitor> GetEnumerator() => ((IEnumerable<IMonitor>)m_Monitors).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_Monitors).GetEnumerator();

        public bool IsEmpty() => m_Monitors.Count == 0;
    }
}
