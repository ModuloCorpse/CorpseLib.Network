using CorpseLib.Network.Http;
using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API
{
    public abstract class AEndpoint(Path path, bool needExactPath) : ResourceSystem.Resource
    {
        private API? m_API = null;
        private readonly Path m_Path = path;
        private readonly bool m_NeedExactPath = needExactPath;

        public Path Path => m_Path;
        public int Port => m_API?.Port ?? -1;
        public bool NeedExactPath => m_NeedExactPath;

        internal void SetAPI(API api) => m_API = api;

        protected AEndpoint(Path path) : this(path, false) { }
    }
}
