using CorpseLib.Serialize;
using System.Net;
using System.Net.Sockets;

namespace CorpseLib.Network
{
    /// <summary>
    /// Class representing a TCP client
    /// </summary>
    public abstract class ATCPClient
    {
        public delegate void Callback(ATCPClient client);
        public event Callback? OnDisconnect;
        public event Callback? OnUnwantedDisconnection;

        protected readonly AProtocol m_Protocol;
        private readonly URI m_URL;
        protected Socket m_Socket;
        protected Stream m_Stream;
        private readonly BytesSerializer m_BytesSerializer = new();
        private readonly BytesReader m_BytesReader;
        private readonly MonitorBatch m_Monitor = [];
        private TimeSpan m_Delay = TimeSpan.Zero;
        private uint m_MaxNbTry = 1;
        private readonly int m_ID;
        private readonly bool m_IsServerSide;
        private volatile bool m_IsReconnecting = false;
        private volatile bool m_IsReconnectionEnabled = false;
        private volatile bool m_IsConnected = false;
        private volatile bool m_IsReadOnly = false;

        protected TimeSpan Delay => m_Delay;
        protected uint MaxNbTry => m_MaxNbTry;

        protected void SetIsReconnecting(bool isReconnecting) => m_IsReconnecting = isReconnecting;

        public URI GetURL() => m_URL;
        public int GetID() => m_ID;
        public bool IsConnected() => m_IsConnected;
        public bool IsReconnecting() => m_IsReconnecting;
        public bool IsServerSide() => m_IsServerSide;

        private ATCPClient(AProtocol protocol, int id)
        {
            m_Protocol = protocol;
            m_Protocol.FillSerializer(ref m_BytesSerializer);
            m_BytesReader = new(m_BytesSerializer);
            protocol.SetClient(this);
            m_ID = id;
            m_URL = URI.Create(string.Empty, -1);
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Stream = Stream.Null;
        }

        public ATCPClient(AProtocol protocol, URI url, int id = 0) : this(protocol, id)
        {
            m_URL = url;
            m_IsServerSide = false;
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Stream = Stream.Null;
        }

        internal ATCPClient(AProtocol protocol, int id, Socket socket) : this(protocol, id)
        {
            m_IsServerSide = true;
            m_IsConnected = true;
            m_Socket = socket;
            IPEndPoint? endpoint = (IPEndPoint?)socket.RemoteEndPoint;
            m_URL = URI.Build(string.Empty)
                .Host(endpoint?.Address?.ToString() ?? string.Empty)
                .Port(endpoint?.Port ?? -1).Build();
            m_Stream = new NetworkStream(socket);
            m_Protocol.ClientConnected();
        }

        public void SetReadOnly(bool isReadOnly) => m_IsReadOnly = isReadOnly;

        internal BytesSerializer BytesSerializer => m_BytesSerializer;

        protected static void Append(ref byte[] dst, byte[] src, int count)
        {
            byte[] rv = new byte[dst.Length + count];
            Buffer.BlockCopy(dst, 0, rv, 0, dst.Length);
            Buffer.BlockCopy(src, 0, rv, dst.Length, count);
            dst = rv;
        }

        protected void ReadAllStream(ref byte[] buffer)
        {
            byte[] readBuffer = new byte[1024];
            int bytesRead;
            try
            {
                while ((bytesRead = m_Stream.Read(readBuffer, 0, readBuffer.Length)) == readBuffer.Length)
                    Append(ref buffer, readBuffer, bytesRead);
                Append(ref buffer, readBuffer, bytesRead);
            }
            catch (Exception ex)
            {
                DiscardException(ex);
            }
        }

        protected void DiscardException(Exception ex)
        {
            m_Protocol.DiscardException(ex);
            m_Monitor.OnException(ex);
        }

        public void AddMonitor(IMonitor monitor)
        {
            m_Monitor.Add(monitor);
            if (m_IsConnected)
                m_Monitor.OnOpen();
        }

        protected abstract Task HandleActionAfterReconnect(Action action);

        private void WaitForReconnection(Action action)
        {
            if (!m_IsReconnecting)
            {
                action();
                return;
            }
            HandleActionAfterReconnect(action);
        }

        public void SetSelfReconnectionDelay(TimeSpan delay) => WaitForReconnection(() => m_Delay = delay);
        public void SetSelfReconnectionMaxNbTry(uint maxNbTry) => WaitForReconnection(() => m_MaxNbTry = maxNbTry);
        public void SetSelfReconnectionDelayAndMaxNbTry(TimeSpan delay, uint maxNbTry) => WaitForReconnection(() =>
        {
            m_Delay = delay;
            m_MaxNbTry = maxNbTry;
        });

        public void EnableSelfReconnection() => m_IsReconnectionEnabled = true;
        public void DisableSelfReconnection() => m_IsReconnectionEnabled = false;

        protected bool InternalConnect(TimeSpan timeout, bool reconnect)
        {
            if (m_IsServerSide)
                return false;
            try
            {
                if (!reconnect)
                    m_Monitor.OnOpening();
                else
                    m_Monitor.OnReopening();
                //Recreate socket and reset stream to allow reconnection
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Stream = Stream.Null;
                foreach (IPAddress ip in Dns.GetHostEntry(m_URL.Host).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IAsyncResult result = m_Socket.BeginConnect(ip, m_URL.Port, null, null);
                        result.AsyncWaitHandle.WaitOne(timeout, true);
                        if (m_Socket.Connected)
                        {
                            m_Socket.EndConnect(result);
                            m_Stream = new NetworkStream(m_Socket);
                            if (!reconnect)
                            {
                                m_Protocol.ClientConnected();
                                m_Monitor.OnOpen();
                            }
                            else
                            {
                                m_Protocol.ClientReconnected();
                                m_Monitor.OnReopen();
                            }
                            m_IsConnected = true;
                            return true;
                        }
                        else
                        {
                            UnwantedDisconnection();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiscardException(ex);
                UnwantedDisconnection();
            }
            return false;
        }

        protected abstract Task HandleReconnect();

        public void Reconnect()
        {
            if (m_IsServerSide)
                return;
            if (!m_IsReconnecting)
            {
                m_BytesReader.Clear();
                m_Socket.Close();
                m_Stream.Close();
                m_Protocol.ClientReset();
                m_IsReconnecting = true;
                HandleReconnect();
            }
        }

        private void InternalReconnect()
        {
            if (m_IsServerSide)
                return;
            if (m_IsReconnectionEnabled)
                Reconnect();
        }

        public bool Connect() => InternalConnect(TimeSpan.FromMinutes(1), false);
        public bool Connect(TimeSpan timeout) => InternalConnect(timeout, false);

        protected List<object> Received(byte[] readBuffer)
        {
            m_Monitor.OnReceive(readBuffer);
            m_BytesReader.Append(readBuffer);
            List<object> packets = [];
            do
            {
                m_BytesReader.LockIdx();
                OperationResult<object> result = m_Protocol.ReadFromStream(m_BytesReader);
                if (result)
                {
                    m_BytesReader.RemoveReadBytes();
                    object? resultData = result.Result;
                    if (resultData != null)
                    {
                        packets.Add(resultData);
                        m_Monitor.OnReceive(resultData);
                        HandleReceivedPacket(resultData);
                    }
                }
                else
                {
                    m_BytesReader.RevertIdx();
                    return packets;
                }
            } while (m_BytesReader.CanRead());
            return packets;
        }

        protected void TestReceived(BytesWriter bytesWriter)
        {
            bool readSuccess;
            BytesReader reader = new(bytesWriter.Serializer, bytesWriter.Bytes);
            do
            {
                reader.LockIdx();
                OperationResult<object> result = m_Protocol.ReadFromStream(reader);
                if (result)
                {
                    reader.RemoveReadBytes();
                    object? resultData = result.Result;
                    if (resultData != null)
                        HandleReceivedPacket(resultData);
                    readSuccess = true;
                }
                else
                {
                    reader.RevertIdx();
                    readSuccess = false;
                }
            } while (reader.CanRead() && readSuccess);
        }

        protected abstract void HandleReceivedPacket(object packet);

        public BytesWriter CreateBytesWriter() => new(m_BytesSerializer);
        public abstract void TestRead(BytesWriter bytesWriter);
        public abstract List<object> Read();

        public void Send(object message)
        {
            if (m_IsReadOnly)
                return;

            m_Monitor.OnSend(message);
            m_Protocol.InternalSend(message);
        }

        internal void ForceSend(object message)
        {
            m_Monitor.OnSend(message);
            m_Protocol.InternalSend(message);
        }

        internal void WriteToStream(byte[] message)
        {
            m_Monitor.OnSend(message);
            try
            {
                m_Stream.Write(message);
                m_Stream.Flush();
            }
            catch (Exception ex)
            {
                DiscardException(ex);
                UnwantedDisconnection();
            }
        }

        internal void SetStream(Stream stream) => m_Stream = stream;
        internal Stream GetStream() => m_Stream;

        protected void UnwantedDisconnection()
        {
            if (Disconnect())
            {
                OnUnwantedDisconnection?.Invoke(this);
                InternalReconnect();
            }
        }

        public bool Disconnect()
        {
            if (m_IsConnected)
            {
                m_IsConnected = false;
                OnDisconnect?.Invoke(this);
                m_Protocol.ClientDisconnected();
                m_Socket.Close();
                m_Stream.Close();
                m_Monitor.OnClose();
                return true;
            }
            return false;
        }

        public abstract bool IsAsynchronous();

        ~ATCPClient() => Disconnect();
    }
}
