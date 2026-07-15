namespace CorpseLib.Network.OAuth
{
    public class AuthenticatorInfoBuilder(string host, string[] scopes, string publicKey, string privateKey)
    {
        private readonly string[] m_Scopes = scopes;
        private readonly string m_Host = host;
        private readonly string m_PublicKey = publicKey;
        private readonly string m_PrivateKey = privateKey;
        private string m_AuthorizePath = "/oauth2/authorize";
        private string m_TokenPath = "/oauth2/token";
        private string m_RedirectPath = "/oauth_authenticate";
        private int m_RedirectPort = 80;

        public AuthenticatorInfoBuilder SetAuthorizePath(string path)
        {
            m_AuthorizePath = path;
            return this;
        }

        public AuthenticatorInfoBuilder SetTokenPath(string path)
        {
            m_TokenPath = path;
            return this;
        }

        public AuthenticatorInfoBuilder SetRedirectPath(string path)
        {
            m_RedirectPath = path;
            return this;
        }

        public AuthenticatorInfoBuilder SetRedirectPort(int port)
        {
            m_RedirectPort = port;
            return this;
        }

        public AuthenticatorInfo Build() => new(URI.Build("http").Host("localhost").Port(m_RedirectPort).Path(m_RedirectPath).Build(), m_Host, m_Scopes, m_PublicKey, m_PrivateKey, m_TokenPath, m_AuthorizePath);
    }
}
