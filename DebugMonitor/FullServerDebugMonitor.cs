using System.Text;

namespace CorpseLib.Network
{
    public class FullServerDebugMonitor : IMonitor
    {
        public void OnClose() => Console.WriteLine("Connection with client closed");
        public void OnOpening() => Console.WriteLine("Connection with client opening");
        public void OnOpen() => Console.WriteLine("Connection with client open");
        public void OnReopening() => Console.WriteLine("Connection with client reopening");
        public void OnReopen() => Console.WriteLine("Connection with client reopenned");
        public void OnReceive(byte[] bytes) => Console.WriteLine($"Received from client {bytes.Length} bytes [UTF-8 : {Encoding.UTF8.GetString(bytes)}]");
        public void OnReceive(object obj) => Console.WriteLine($"Received from client: {obj}");
        public void OnSend(object obj) => Console.WriteLine($"Sent to client: {obj}");
        public void OnSend(byte[] bytes) => Console.WriteLine($"Sent to client {bytes.Length} bytes [UTF-8 : {Encoding.UTF8.GetString(bytes)}]");
        public void OnException(Exception ex) => Console.WriteLine(ex);
        public void OnLog(string log) => Console.WriteLine(log);
    }
}
