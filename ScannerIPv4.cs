using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;

namespace CorpseLib.Network
{
    public class ScannerIPv4
    {
        public class Result(URI url, string hostName, object? metadata)
        {
            private readonly URI m_URL = url;
            private readonly object? m_Metadata = metadata;
            private readonly string m_HostName = hostName;
            public URI URL => m_URL;
            public object? Metadata => m_Metadata;
            public string HostName => m_HostName;
        }

        private class ScannerData(IPTest? iPTest)
        {
            private readonly Guid m_CounterGuid = ThreadCounter.NewCounter();
            private readonly ConcurrentBag<Result> m_IPS = [];
            private readonly IPTest? m_IPTest = iPTest;

            public void AddPing() => ThreadCounter.IncreaseCounter(m_CounterGuid);
            public void PingCompleted() => ThreadCounter.DecreaseCounter(m_CounterGuid);
            public void WaitForAllPings() => ThreadCounter.Wait(m_CounterGuid, 0, 100);

            public void AddIP(string hostName, string host)
            {
                IPTestResult result = [];
                if (m_IPTest?.Invoke(host, result) ?? true)
                {
                    if (result.Count == 0)
                        m_IPS.Add(new(URI.Build(string.Empty).Host(host).Build(), hostName, null));
                    foreach (IPTestResult.IPData port in result)
                        m_IPS.Add(new(URI.Build(string.Empty).Host(host).Port(port.Port).Build(), hostName, port.Data));
                }
            }

            public ConcurrentBag<Result> GetResult() => m_IPS;
        }

        public class IPTestResult : IEnumerable<IPTestResult.IPData>
        {
            public class IPData(int port, object? data)
            {
                private readonly object? m_Data = data;
                private readonly int m_Port = port;
                public object? Data => m_Data;
                public int Port => m_Port;

            }

            private readonly List<IPData> m_Datas = [];

            public int Count => m_Datas.Count;

            public void ValidatePort(int port) => m_Datas.Add(new(port, null));
            public void ValidatePort(int port, object? data) => m_Datas.Add(new(port, data));

            public IEnumerator<IPData> GetEnumerator() => ((IEnumerable<IPData>)m_Datas).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_Datas).GetEnumerator();
        }

        public delegate bool IPTest(string ip, IPTestResult result);

        public static readonly ScannerIPv4 LOCAL_A = new("10.[].[].[]");
        public static readonly ScannerIPv4 LOCAL_B = new("172.[16-31].[].[]");
        public static readonly ScannerIPv4 LOCAL_C = new("192.168.[].[]");
        public static readonly ScannerIPv4 LOCAL_NETWORK = new("192.168.1.[]");

        private readonly List<byte> m_FirstRange;
        private readonly List<byte> m_SecondRange;
        private readonly List<byte> m_ThirdRange;
        private readonly List<byte> m_FourthRange;

        private static List<byte> GetRange(string range)
        {
            List<byte> ret = [];
            if (range[0] == '[' && range[^1] == ']')
            {
                if (range == "[]")
                {
                    for (byte i = 0; i < 255; ++i)
                        ret.Add(i);
                    ret.Add(255);
                }
                else
                {
                    string[] ranges = range[1..^1].Split('/');
                    foreach (string ipRange in ranges)
                    {
                        if (ipRange.Contains('-'))
                        {
                            string[] dynamicRange = ipRange[1..^1].Split('-');
                            if (dynamicRange.Length != 2)
                                throw new ArgumentException($"{range} is not a valid range");
                            byte min = byte.Parse(dynamicRange[0]);
                            byte max = byte.Parse(dynamicRange[1]);
                            for (byte i = min; i < max; ++i)
                                ret.Add(i);
                            ret.Add(max);
                        }
                        else if (byte.TryParse(ipRange, out byte value))
                            ret.Add(value);
                    }
                }
            }
            else
                ret.Add(byte.Parse(range));
            return ret;
        }

        public ScannerIPv4(string scanRange)
        {
            string[] ranges = scanRange.Split('.');
            if (ranges.Length != 4)
                throw new ArgumentException($"{scanRange} is not a valid scanner range");
            m_FirstRange = GetRange(ranges[0]);
            m_SecondRange = GetRange(ranges[1]);
            m_ThirdRange = GetRange(ranges[2]);
            m_FourthRange = GetRange(ranges[3]);
        }

        private void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            ScannerData data = (ScannerData)e.UserState!;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                string ip = e.Reply.Address.ToString();
                string hostName = string.Empty;
                try
                {
                    IPHostEntry entry = Dns.GetHostEntry(ip);
                    if (entry != null)
                        hostName = entry.HostName;
                }
                catch { }
                data.AddIP(hostName, ip);
            }
            data.PingCompleted();
        }

        private void IterateOverFourthRange(byte i, byte j, byte k, ConcurrentBag<Result> results, IPTest? ipTest)
        {
            ScannerData scannerData = new(ipTest);
            foreach (byte l in m_FourthRange)
            {
                string ip = $"{i}.{j}.{k}.{l}";
                Ping ping = new();
                ping.PingCompleted += new PingCompletedEventHandler(PingCompleted);
                scannerData.AddPing();
                ping.SendAsync(ip, 100, scannerData);
            }
            scannerData.WaitForAllPings();
            IReadOnlyCollection<Result> dataResult = scannerData.GetResult();
            foreach (Result result in dataResult)
                results.Add(result);
        }

        private void IterateOverThirdRange(byte i, byte j, ConcurrentBag<Result> results, IPTest? ipTest)
        {
            Guid counterGuid = ThreadCounter.NewCounter();
            ThreadCounter.IncreaseCounter(counterGuid);
            Task.Run(() =>
            {
                foreach (byte k in m_ThirdRange)
                {
                    ThreadCounter.IncreaseCounter(counterGuid);
                    IterateOverFourthRange(i, j, k, results, ipTest);
                    ThreadCounter.DecreaseCounter(counterGuid);
                }
                ThreadCounter.DecreaseCounter(counterGuid);
            });
            ThreadCounter.Wait(counterGuid, 0, 100);
        }

        private void IterateOverSecondRange(byte i, ConcurrentBag<Result> results, IPTest? ipTest)
        {
            Guid counterGuid = ThreadCounter.NewCounter();
            ThreadCounter.IncreaseCounter(counterGuid);
            Task.Run(() =>
            {
                foreach (byte j in m_SecondRange)
                {
                    ThreadCounter.IncreaseCounter(counterGuid);
                    IterateOverThirdRange(i, j, results, ipTest);
                    ThreadCounter.DecreaseCounter(counterGuid);
                }
                ThreadCounter.DecreaseCounter(counterGuid);
            });
            ThreadCounter.Wait(counterGuid, 0, 100);
        }

        private void IterateOverFirstRange(ConcurrentBag<Result> results, IPTest? ipTest)
        {
            Guid counterGuid = ThreadCounter.NewCounter();
            ThreadCounter.IncreaseCounter(counterGuid);
            Task.Run(() =>
            {
                foreach (byte i in m_FirstRange)
                {
                    ThreadCounter.IncreaseCounter(counterGuid);
                    IterateOverSecondRange(i, results, ipTest);
                    ThreadCounter.DecreaseCounter(counterGuid);
                }
                ThreadCounter.DecreaseCounter(counterGuid);
            });
            ThreadCounter.Wait(counterGuid, 0, 100);
        }

        public IReadOnlyCollection<Result> Scan(IPTest? ipTest = null)
        {
            ConcurrentBag<Result> results = [];
            IterateOverFirstRange(results, ipTest);
            List<Result> ret = [];
            while (results.TryTake(out Result? result))
                ret.Add(result);
            ret.Sort((x, y) => string.Compare(x.URL.Host, y.URL.Host));
            return ret.AsReadOnly();
        }
    }
}
