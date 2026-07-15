using CorpseLib.DataNotation;
using CorpseLib.Json;
using CorpseLib.Network.OAuth;

namespace CorpseLib.Network.Http
{
    public class URLRequest
    {
        private readonly static HttpClient ms_Client = new();
        private readonly Request m_Request;

        public URLRequest(URI url, Request.MethodType method = Request.MethodType.GET)
        {
            m_Request = new(method, url);
            m_Request["Host"] = url.Host;
        }

        public URLRequest(URI url, Request.MethodType method, string content): this(url, method) => m_Request.SetBody(content);

        public URLRequest(URI url, Request.MethodType method, DataObject content) : this(url, method)
        {
            m_Request.SetBody(JsonParser.NetStr(content));
            m_Request["Content-Type"] = MIME.APPLICATION.JSON.ToString();
        }

        public void AddHeaderField(string field, string value) => m_Request[field] = value;

        public void AddContentType(MIME mime) => m_Request["Content-Type"] = mime.ToString();

        public void AddRefreshToken(Token token)
        {
            m_Request["Authorization"] = $"Bearer {token.AccessToken}";
            m_Request["Client-Id"] = token.ClientID;
        }

        public Request Request => m_Request;

        public Response Send() => Send(TimeSpan.FromSeconds(60));
        public Response Send(TimeSpan timeout)
        {
            CancellationTokenSource cts = new(timeout);
            HttpRequestMessage requestMessage = m_Request.ToHttpRequestMessage();
            HttpResponseMessage response = ms_Client.Send(requestMessage, cts.Token);
            return new(response);
        }
    }
}
