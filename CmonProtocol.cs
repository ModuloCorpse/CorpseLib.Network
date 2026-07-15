using CorpseLib.DataNotation;
using CorpseLib.Network;
using CorpseLib.Serialize;
using System.Text;

namespace CorpseLib.Cmon
{
    public abstract class CmonProtocol : AProtocol
    {
        private string m_Buffer = string.Empty;

        protected override void OnClientConnected() { }
        protected override void OnClientReconnected() { }
        protected override void OnClientDisconnected() { }
        protected override void OnClientReset() { }
        protected override void SetupSerializer(ref BytesSerializer serializer) { }

        protected sealed override void Write(BytesWriter writer, object packet)
        {
            if (packet is DataObject obj)
                writer.Write(Encoding.UTF8.GetBytes(CmonParser.NetStr(obj)));
            else if (packet is string str)
                writer.Write(Encoding.UTF8.GetBytes(str));
            else
                writer.Write(packet);
        }

        protected sealed override OperationResult<object> Read(BytesReader reader)
        {
            int originalBufferLength = Encoding.UTF8.GetByteCount(m_Buffer);
            m_Buffer += Encoding.UTF8.GetString(reader.Bytes);
            int i = 0;
            while (i < m_Buffer.Length && char.IsWhiteSpace(m_Buffer[i]))
                i++;
            if (i == m_Buffer.Length || m_Buffer[i] != '{')
                return new("Invalid CMON", "Received CMON doesn't start with a {");
            bool inString = false;
            i++;
            int scope = 1;
            while (i < m_Buffer.Length && scope != 0)
            {
                if (inString)
                {
                    if (m_Buffer[i] == '"')
                        inString = false;
                }
                else
                {
                    switch (m_Buffer[i])
                    {
                        case '"': inString = true; break;
                        case '{': ++scope; break;
                        case '}': --scope; break;
                    }
                }
                ++i;
            }
            if (scope != 0)
            {
                _ = reader.ReadAll();
                return new(null);
            }
            string jsonString = m_Buffer[..i];
            int readLength = Encoding.UTF8.GetByteCount(jsonString) - originalBufferLength;
            try
            {
                DataObject node = CmonParser.Parse(jsonString);
                _ = reader.ReadBytes(readLength);
                m_Buffer = string.Empty;
                return new(node);
            }
            catch (CmonException e)
            {
                return new("Deserialization error", e.Message);
            }
        }

        protected sealed override void Treat(object packet) => OnReceive((DataObject)packet);

        protected abstract void OnReceive(DataObject obj);
    }
}
