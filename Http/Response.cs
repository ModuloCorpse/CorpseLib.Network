using System.Net;
using System.Text;

namespace CorpseLib.Network.Http
{
    /// <summary>
    /// An HTTP response
    /// </summary>
    public class Response : AMessage
    {
        private readonly Version m_Version;
        private readonly int m_StatusCode;
        private readonly string m_StatusMessage;

        internal Response(HttpResponseMessage response)
        {
            m_Version = new(response.Version);
            m_StatusCode = (int)response.StatusCode;
            m_StatusMessage = response.ReasonPhrase ?? string.Empty;
            foreach (var header in response.Headers)
                base[header.Key] = string.Join("+", header.Value);
            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                    base[header.Key] = string.Join("+", header.Value);
                Task<byte[]> stringTask = response.Content.ReadAsByteArrayAsync();
                stringTask.GetAwaiter().GetResult();
                SetBody(stringTask.Result);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="response">Content of the received HTTP response</param>
        public Response(string response)
        {
            List<string> attributes = [.. response.Split(separator, StringSplitOptions.None)];
            string statusLine = attributes[0].Trim();
            int indexOfSeparator = statusLine.IndexOf(' ');
            string versionString = statusLine[..indexOfSeparator][5..];
            m_Version = new(versionString);
            statusLine = statusLine[(indexOfSeparator + 1)..];
            indexOfSeparator = statusLine.IndexOf(' ');
            m_StatusCode = int.Parse(statusLine[..indexOfSeparator]);
            m_StatusMessage = statusLine[(indexOfSeparator + 1)..];
            attributes.RemoveAt(0);
            ParseHeaderFields(attributes);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        /// <param name="contentType">MIME type of the content to send</param>
        public Response(int statusCode, string statusMessage, byte[] body, MIME contentType)
        {
            m_Version = new(1, 1);
            m_StatusCode = statusCode;
            m_StatusMessage = statusMessage;
            SetBody(body);
            if (body.Length > 0)
            {
                if (contentType.HaveParameter())
                    base["Content-Type"] = contentType.ToString();
                else
                    base["Content-Type"] = $"{contentType}; charset=utf-8";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        public Response(int statusCode, string statusMessage, byte[] body)
        {
            m_Version = new(1, 1);
            m_StatusCode = statusCode;
            m_StatusMessage = statusMessage;
            SetBody(body);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        /// <param name="contentType">MIME type of the content to send</param>
        public Response(int statusCode, string statusMessage, string body, MIME contentType) : this(statusCode, statusMessage, Encoding.UTF8.GetBytes(body), contentType) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        public Response(int statusCode, string statusMessage, string body) : this(statusCode, statusMessage, Encoding.UTF8.GetBytes(body)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        public Response(int statusCode, string statusMessage) : this(statusCode, statusMessage, Array.Empty<byte>()) { }

        protected override string GetHeader() => $"HTTP/{m_Version} {m_StatusCode} {m_StatusMessage}";

        internal void ToHttpResponseMessage(HttpListenerResponse response)
        {
            response.ProtocolVersion = new(m_Version.Major, m_Version.Minor);
            response.StatusCode = m_StatusCode;
            response.StatusDescription = m_StatusMessage;

            foreach (var header in Fields)
            {
                string key = header.Key;
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    response.Headers[key] = header.Value.ToString();
                }
                catch { /* Certains headers sont réservés/protégés */ }
            }

            if (RawBody != null && RawBody.Length > 0)
            {
                response.ContentLength64 = RawBody.Length;
                response.OutputStream.Write(RawBody, 0, RawBody.Length);
                response.OutputStream.Flush();
            }
        }

        /// <summary>
        /// HTTP version of the response
        /// </summary>
        public Version Version => m_Version;

        /// <summary>
        /// Status code of the response
        /// </summary>
        public int StatusCode => m_StatusCode;

        /// <summary>
        /// Status message of the response
        /// </summary>
        public string StatusMessage => m_StatusMessage;

        private static readonly string[] separator = ["\r\n"];
    }
}
