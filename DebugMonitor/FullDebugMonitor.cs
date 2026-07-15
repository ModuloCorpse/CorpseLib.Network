using System.Text;

namespace CorpseLib.Network
{
    public class FullDebugMonitor : IMonitor
    {
        public void OnClose() => Console.WriteLine("Connection closed");
        public void OnOpening() => Console.WriteLine("Connection opening");
        public void OnOpen() => Console.WriteLine("Connection open");
        public void OnReopening() => Console.WriteLine("Connection reopening");
        public void OnReopen() => Console.WriteLine("Connection reopenned");
        public void OnReceive(byte[] bytes) => Console.WriteLine($"Received {bytes.Length} bytes [UTF-8 : {Encoding.UTF8.GetString(bytes)}]");
        public void OnReceive(object obj) => Console.WriteLine($"Received: {obj}");
        public void OnSend(object obj) => Console.WriteLine($"Sent: {obj}");
        public void OnSend(byte[] bytes) => Console.WriteLine($"Sent {bytes.Length} bytes [UTF-8 : {Encoding.UTF8.GetString(bytes)}]");
        public void OnException(Exception ex) => Console.WriteLine(ex);
        public void OnLog(string log) => Console.WriteLine(log);
    }
}
