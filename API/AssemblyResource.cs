using CorpseLib.Network.Http;
using System.Reflection;

namespace CorpseLib.Network.API
{
    public class AssemblyResource(Http.Path path, bool needExactPath, Assembly? assembly, string assemblyPath, MIME? mime = null) : AHTTPEndpoint(path, needExactPath)
    {
        private Assembly? m_Assembly = assembly;
        private readonly MIME? m_MIME = mime;
        private readonly string m_AssemblyPath = assemblyPath;

        public AssemblyResource(Http.Path path, Assembly? assembly, string assemblyPath, MIME? mime = null) : this(path, false, assembly, assemblyPath, mime) { }

        private static byte[] ReadFully(Stream input)
        {
            using MemoryStream ms = new();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        protected override Response OnGetRequest(Request request)
        {
            if (m_Assembly != null)
            {
                Stream? internalResourceStream = m_Assembly.GetManifestResourceStream(m_AssemblyPath);
                if (internalResourceStream != null)
                {
                    MIME? mime = m_MIME ?? MIME.GetMIME(m_AssemblyPath);
                    if (mime != null)
                        return new Response(200, "Ok", ReadFully(internalResourceStream), mime);
                    return new Response(200, "Ok", ReadFully(internalResourceStream));
                }
            }
            else
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    Stream? internalResourceStream = assembly.GetManifestResourceStream(m_AssemblyPath);
                    if (internalResourceStream != null)
                    {
                        m_Assembly = assembly;
                        MIME? mime = m_MIME ?? MIME.GetMIME(m_AssemblyPath);
                        if (mime != null)
                            return new Response(200, "Ok", ReadFully(internalResourceStream), mime);
                        return new Response(200, "Ok", ReadFully(internalResourceStream));
                    }
                }
            }
            return new(404, "Not Found", $"{request.Path} does not exist");
        }
    }
}
