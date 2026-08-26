using CorpseLib.Encryption;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CorpseLib.Network.OAuth
{
    public class Authenticator(AuthenticatorInfo info, OAuthEndpoint endpoint)
    {
        [SupportedOSPlatform("windows")]
        private readonly WindowsEncryptionAlgorithm m_WindowsEncryptionAlgorithm = new([95, 239, 5, 252, 160, 29, 242, 88, 31, 3]);
        private readonly OAuthEndpoint m_Endpoint = endpoint;
        private readonly AuthenticatorInfo m_AuthenticatorInfo = info;

        public AuthenticatorInfo AuthenticatorInfo => m_AuthenticatorInfo;

        public void SetPageContent(string content) => m_Endpoint.SetPageContent(content);

        public OperationResult<RefreshToken> ClientCredentials() => new(new(m_AuthenticatorInfo.TokenURI, m_AuthenticatorInfo.PublicKey, m_AuthenticatorInfo.PrivateKey));

        public async Task<OperationResult<RefreshToken>> AuthorizationCode(string browser = "")
        {
            OAuthRequest request = new(m_AuthenticatorInfo);
            URI oauthURL = m_AuthenticatorInfo.GetRequestURL(request);
            m_Endpoint.RequestToken(request);

            Process myProcess = new();
            myProcess.StartInfo.UseShellExecute = true;
            if (string.IsNullOrWhiteSpace(browser))
                myProcess.StartInfo.FileName = oauthURL.ToString();
            else
            {
                myProcess.StartInfo.FileName = browser;
                myProcess.StartInfo.Arguments = oauthURL.ToString();
            }
            myProcess.Start();
            return await request.WaitResult();
        }

        public void SaveToken(string path, RefreshToken token)
        {
            string content = $"{token.AccessToken}\n{token.TokenRefresh}";
            if (OperatingSystem.IsWindows())
            {
                EncryptedFile encryptedFile = new(path) { m_WindowsEncryptionAlgorithm };
                encryptedFile.Write(content);
            }
            else
                File.WriteAllText(path, content);
        }

        public RefreshToken? LoadToken(string path)
        {
            string content;
            if (OperatingSystem.IsWindows())
            {
                EncryptedFile encryptedFile = new(path) { m_WindowsEncryptionAlgorithm };
                content = encryptedFile.Read();
            }
            else
                content = File.ReadAllText(path);
            string[] lines = content.Split('\n');
            if (lines.Length == 2)
            {
                RefreshToken ret = new(m_AuthenticatorInfo.Scopes, m_AuthenticatorInfo.PrivateKey, m_AuthenticatorInfo.TokenURI, lines[1], m_AuthenticatorInfo.PublicKey, lines[0]);
                if (ret.Refresh())
                    return ret;
            }
            return null;
        }

        public RefreshToken? LoadToken(LocalVault vault, string key)
        {
            string content = vault.Load(key);
            string[] lines = content.Split('\n');
            if (lines.Length == 2)
            {
                RefreshToken ret = new(m_AuthenticatorInfo.Scopes, m_AuthenticatorInfo.PrivateKey, m_AuthenticatorInfo.TokenURI, lines[1], m_AuthenticatorInfo.PublicKey, lines[0]);
                if (ret.Refresh())
                    return ret;
            }
            return null;
        }

        public RefreshToken CreateToken(string access, string refresh) => new(m_AuthenticatorInfo.Scopes, m_AuthenticatorInfo.PrivateKey, m_AuthenticatorInfo.TokenURI, refresh, m_AuthenticatorInfo.PublicKey, access);
    }
}
