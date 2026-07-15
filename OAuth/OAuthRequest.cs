namespace CorpseLib.Network.OAuth
{
    internal class OAuthRequest(AuthenticatorInfo authenticatorInfo)
    {
        private readonly Operation<RefreshToken> m_RefreshTokenOperation = new();
        private readonly AuthenticatorInfo m_AuthenticatorInfo = authenticatorInfo;
        private readonly string m_State = Guid.NewGuid().ToString();

        public string State => m_State;

        public bool MatchScope(IEnumerable<string> scopes) => m_AuthenticatorInfo.MatchScope(scopes);
        public void SetResult(string token) => m_RefreshTokenOperation.SetResult(new(m_AuthenticatorInfo.TokenURI, m_AuthenticatorInfo.Scopes, m_AuthenticatorInfo.PublicKey, m_AuthenticatorInfo.PrivateKey, token, m_AuthenticatorInfo.RedirectURI.ToString()));
        public void SetError(string error, string description) => m_RefreshTokenOperation.SetError(error, description);
    }
}
