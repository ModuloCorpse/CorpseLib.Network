namespace CorpseLib.Network.OAuth
{
    public class AuthenticatorInfo(URI redirectURI, string host, string[] scopes, string publicKey, string privateKey, string tokenPath, string authorizePath)
    {
        private readonly URI m_RedirectURI = redirectURI;
        private readonly URI m_TokenURI = URI.Build("https").Host(host).Port(443).Path(tokenPath).Build();
        private readonly string[] m_Scopes = scopes;
        private readonly string m_AuthorizePath = authorizePath;
        private readonly string m_Host = host;
        private readonly string m_PublicKey = publicKey;
        private readonly string m_PrivateKey = privateKey;

        public URI RedirectURI => m_RedirectURI;
        public URI TokenURI => m_TokenURI;
        public string[] Scopes => m_Scopes;
        public string PublicKey => m_PublicKey;
        public string PrivateKey => m_PrivateKey;
        public string AuthorizePath => m_AuthorizePath;

        internal bool MatchScope(IEnumerable<string> scopes) => m_Scopes.All(item => scopes.Contains(item)) && scopes.All(item => m_Scopes.Contains(item));
        internal URI GetRequestURL(OAuthRequest request) => URI.Build("https")
                .Host(m_Host)
                .Path(m_AuthorizePath)
                .Query(new URIQuery('&')
                {
                    { "response_type", "code" },
                    { "client_id", m_PublicKey },
                    { "redirect_uri", m_RedirectURI.ToString() },
                    { "scope", string.Join('+', m_Scopes).Replace(":", "%3A") },
                    { "state", request.State }
                }).Build();
    }
}
