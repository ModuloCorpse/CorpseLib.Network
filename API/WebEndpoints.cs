namespace CorpseLib.Network.API
{
    public class WebEndpoints(AHTTPEndpoint httpEndpoint, AWebsocketEndpoint wsEndpoint, Http.Path path, bool needExactPath = true) : AEndpoint(path, needExactPath)
    {
        private readonly AHTTPEndpoint m_HTTPEndpoint = httpEndpoint;
        private readonly AWebsocketEndpoint m_WebsocketEndpoint = wsEndpoint;

        public AHTTPEndpoint HTTPEndpoint => m_HTTPEndpoint;
        public AWebsocketEndpoint WebsocketEndpoint => m_WebsocketEndpoint;
    }
}
