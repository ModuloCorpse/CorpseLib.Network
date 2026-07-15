using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace CorpseLib.Network.Http
{
    /// <summary>
    /// An HTTP request
    /// </summary>
    public class Request : AMessage
    {
        public enum MethodType
        {
            GET,
            HEAD,
            POST,
            PUT,
            DELETE,
            CONNECT,
            OPTIONS,
            TRACE,
            PATCH,
            UNDEFINED
        }

        private readonly MethodType m_Method;
        private readonly URI m_URI;
        private readonly Path m_Path;
        private readonly Path m_FullPath;
        private readonly Version m_Version;

        internal Request(HttpListenerRequest request)
        {
            m_Method = request.HttpMethod switch
            {
                "GET" => MethodType.GET,
                "HEAD" => MethodType.HEAD,
                "POST" => MethodType.POST,
                "PUT" => MethodType.PUT,
                "DELETE" => MethodType.DELETE,
                "CONNECT" => MethodType.CONNECT,
                "OPTIONS" => MethodType.OPTIONS,
                "TRACE" => MethodType.TRACE,
                "PATCH" => MethodType.PATCH,
                _ => MethodType.UNDEFINED
            };
            m_URI = URI.Parse(request.Url?.AbsoluteUri ?? string.Empty);
            m_Path = new(m_URI.Path);
            m_FullPath = new(m_URI.FullPath);
            m_Version = new(request.ProtocolVersion);
            NameValueCollection headers = request.Headers;
            foreach (string key in headers)
                base[key] = string.Join("+", headers[key]);

            if (request.HasEntityBody)
            {
                using MemoryStream ms = new();
                request.InputStream.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                if (bytes.Length > 0)
                    SetBody(bytes);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="request">Content of the received HTTP request</param>
        public Request(string request)
        {
            List<string> attributes = [.. request.Split(separator, StringSplitOptions.None)];
            string[] requestLine = attributes[0].Trim().Split(' ');

            m_Method = (requestLine[0]) switch
            {
                "GET" => MethodType.GET,
                "HEAD" => MethodType.HEAD,
                "POST" => MethodType.POST,
                "PUT" => MethodType.PUT,
                "DELETE" => MethodType.DELETE,
                "CONNECT" => MethodType.CONNECT,
                "OPTIONS" => MethodType.OPTIONS,
                "TRACE" => MethodType.TRACE,
                "PATCH" => MethodType.PATCH,
                _ => MethodType.UNDEFINED
            };
            m_Version = new(requestLine[2][5..]);
            attributes.RemoveAt(0);
            ParseHeaderFields(attributes);
            m_URI = URI.Build().Path(requestLine[1]).Build();
            m_Path = new(m_URI.Path);
            m_FullPath = new(m_URI.FullPath);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">Method of the request</param>
        /// <param name="path">URL targeted by the request</param>
        /// <param name="body">Body of the request</param>
        public Request(MethodType method, URI uri, byte[] body)
        {
            m_Method = method;
            m_URI = uri;
            m_Path = new(m_URI.Path);
            m_FullPath = new(m_URI.FullPath);
            m_Version = new(1, 1);
            SetBody(body);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">Method of the request</param>
        /// <param name="path">URL targeted by the request</param>
        /// <param name="body">Body of the request</param>
        public Request(MethodType method, URI uri, string body = "") : this(method, uri, Encoding.UTF8.GetBytes(body)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">Method of the request</param>
        /// <param name="path">URL targeted by the request</param>
        /// <param name="body">Body of the request</param>
        public Request(MethodType method, URI uri)
        {
            m_Method = method;
            m_URI = uri;
            m_Path = new(m_URI.Path);
            m_FullPath = new(m_URI.FullPath);
            m_Version = new(1, 1);
        }

        internal HttpRequestMessage ToHttpRequestMessage()
        {
            HttpRequestMessage request = new((m_Method) switch
            {
                Request.MethodType.GET => HttpMethod.Get,
                Request.MethodType.HEAD => HttpMethod.Head,
                Request.MethodType.POST => HttpMethod.Post,
                Request.MethodType.PUT => HttpMethod.Put,
                Request.MethodType.DELETE => HttpMethod.Delete,
                Request.MethodType.CONNECT => HttpMethod.Connect,
                Request.MethodType.OPTIONS => HttpMethod.Options,
                Request.MethodType.TRACE => HttpMethod.Trace,
                Request.MethodType.PATCH => HttpMethod.Patch,
                _ => HttpMethod.Get,
            }, new Uri(m_URI.ToString()));
            foreach (var field in Fields)
            {
                string key = field.Key;
                string val = field.Value.ToString() ?? string.Empty;

                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Upgrade", StringComparison.OrdinalIgnoreCase))
                    continue;

                request.Headers.TryAddWithoutValidation(key, val);
            }

            if (Body.Length > 0)
            {
                request.Content = new ByteArrayContent(RawBody);
                if (Fields.TryGetValue("Content-Type", out object? value))
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", value.ToString());
            }

            return request;
        }

        private string GetPath() => string.IsNullOrEmpty(m_URI.Path) ? "/" : m_URI.Path;

        protected override string GetHeader() => m_Method switch
        {
            MethodType.GET => $"GET {GetPath()} HTTP/{m_Version}",
            MethodType.HEAD => $"HEAD {GetPath()} HTTP/{m_Version}",
            MethodType.POST => $"POST {GetPath()} HTTP/{m_Version}",
            MethodType.PUT => $"PUT {GetPath()} HTTP/{m_Version}",
            MethodType.DELETE => $"DELETE {GetPath()} HTTP/{m_Version}",
            MethodType.CONNECT => $"CONNECT {GetPath()} HTTP/{m_Version}",
            MethodType.OPTIONS => $"OPTIONS {GetPath()} HTTP/{m_Version}",
            MethodType.TRACE => $"TRACE {GetPath()} HTTP/{m_Version}",
            MethodType.PATCH => $"PATCH {GetPath()} HTTP/{m_Version}",
            _ => throw new ArgumentException()
        };
        /// <summary>
        /// Check if the request contains the given URL parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>True if the URL parameter exists</returns>
        public bool HasParameter(string parameterName) => m_FullPath.HasParameter(parameterName);

        /// <summary>
        /// Get the URL parameter value of the given parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>The value of the URL parameter</returns>
        public string GetParameter(string parameterName) => m_FullPath[parameterName];

        /// <summary>
        /// Get the URL parameter value of the given parameter if it exist
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <param name="value">Container for the value of the parameter if found</param>
        /// <returns>True if it found a value to the given parameter</returns>
        public bool TryGetParameter(string parameterName, out string? value) => m_FullPath.TryGetParameter(parameterName, out value);

        /// <summary>
        /// Method of the request
        /// </summary>
        public MethodType Method => m_Method;
        /// <summary>
        /// URL targeted by the request
        /// </summary>
        public URI Uri => m_URI;
        /// <summary>
        /// Path of the URL targeted by the request
        /// </summary>
        public Path Path => m_Path;
        /// <summary>
        /// HTTP version of the request
        /// </summary>
        public Version Version => m_Version;

        private static readonly string[] separator = ["\r\n"];
    }
}
