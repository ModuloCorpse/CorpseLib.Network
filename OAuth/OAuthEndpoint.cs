using CorpseLib.Network.API;
using CorpseLib.Network.Http;
using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.OAuth
{
    public class OAuthEndpoint(Path path) : AHTTPEndpoint(path)
    {
        private readonly Dictionary<string, OAuthRequest> m_StateOperations = [];
        private string m_PageContent = string.Empty;

        protected override async Task<Response> OnGetRequest(Request request)
        {
            if (request.TryGetParameter("state", out string? state))
            {
                if (m_StateOperations.TryGetValue(state!, out var tokenOperation))
                {
                    List<string> scopeList = [];
                    if (request.TryGetParameter("scope", out string? scope))
                        scopeList.AddRange(scope!.Replace("%3A", ":").Split('+'));
                    string[] scopes = [.. scopeList];
                    if (request.TryGetParameter("code", out string? token) && tokenOperation.MatchScope(scopes))
                    {
                        tokenOperation.SetResult(token!);
                        return new(200, m_PageContent);
                    }
                    else if (request.TryGetParameter("error", out string? error))
                    {
                        if (request.TryGetParameter("error_description", out string? errorDescription))
                            tokenOperation.SetError(error!, errorDescription!.Replace('+', ' '));
                        else
                            tokenOperation.SetError(error!, string.Empty);
                        return new(400, "Bad Request"); //TODO Replace with a page that shows the error and description
                    }
                    else
                        return new(400, "Bad Request", "Missing code or error query parameter");
                }
                else
                    return new(404, "Not Found", $"No request with state '{state!}' found");
            }
            else
                return new(400, "Bad Request", "Missing state query parameter");
        }

        internal void RequestToken(OAuthRequest request) => m_StateOperations.Add(request.State, request);

        public void SetPageContent(string content) => m_PageContent = content;
    }
}
