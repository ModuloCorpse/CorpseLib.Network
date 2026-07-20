using CorpseLib.Network.Http;

namespace CorpseLib.Network.API
{
    public class LocalFileResource(Http.Path path, string filePath, MIME? mime = null) : AHTTPEndpoint(path)
    {
        private readonly MIME? m_MIME = mime;
        private readonly string m_FilePath = filePath;

        protected override async Task<Response> OnGetRequest(Request request)
        {
            if (File.Exists(m_FilePath))
            {
                MIME? mime = m_MIME ?? MIME.GetMIME(m_FilePath);
                if (mime != null)
                    return new Response(200, "Ok", File.ReadAllBytes(m_FilePath), mime);
                return new Response(200, "Ok", File.ReadAllBytes(m_FilePath));
            }
            return new(404, "Not Found", $"{request.Path} does not exist");
        }
    }
}
