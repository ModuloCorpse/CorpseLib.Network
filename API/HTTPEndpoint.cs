using CorpseLib.Network.Http;

namespace CorpseLib.Network.API
{
    public class HTTPEndpoint(Http.Path path, bool needExactPath) : AHTTPEndpoint(path, needExactPath)
    {
        public delegate Response MethodHandler(Request request);

        private MethodHandler? m_Connect = null;
        private MethodHandler? m_Delete = null;
        private MethodHandler? m_Get = null;
        private MethodHandler? m_Head = null;
        private MethodHandler? m_Options = null;
        private MethodHandler? m_Patch = null;
        private MethodHandler? m_Post = null;
        private MethodHandler? m_Put = null;
        private MethodHandler? m_Trace = null;

        public void SetEndpoint(Request.MethodType methodType, MethodHandler methodHandler)
        {
            switch (methodType)
            {
                case Request.MethodType.GET: m_Get = methodHandler; break;
                case Request.MethodType.HEAD: m_Head = methodHandler; break;
                case Request.MethodType.POST: m_Post = methodHandler; break;
                case Request.MethodType.PUT: m_Put = methodHandler; break;
                case Request.MethodType.DELETE: m_Delete = methodHandler; break;
                case Request.MethodType.CONNECT: m_Connect = methodHandler; break;
                case Request.MethodType.OPTIONS: m_Options = methodHandler; break;
                case Request.MethodType.TRACE: m_Trace = methodHandler; break;
                case Request.MethodType.PATCH: m_Patch = methodHandler; break;
            }
        }

        protected override async Task<Response> OnConnectRequest(Request request) => (m_Connect != null) ? m_Connect(request) : await base.OnConnectRequest(request);
        protected override async Task<Response> OnDeleteRequest(Request request) => (m_Delete != null) ? m_Delete(request) : await base.OnDeleteRequest(request);
        protected override async Task<Response> OnGetRequest(Request request) => (m_Get != null) ? m_Get(request) : await base.OnGetRequest(request);
        protected override async Task<Response> OnHeadRequest(Request request) => (m_Head != null) ? m_Head(request) : await base.OnHeadRequest(request);
        protected override async Task<Response> OnOptionsRequest(Request request) => (m_Options != null) ? m_Options(request) : await base.OnOptionsRequest(request);
        protected override async Task<Response> OnPatchRequest(Request request) => (m_Patch != null) ? m_Patch(request) : await base.OnPatchRequest(request);
        protected override async Task<Response> OnPostRequest(Request request) => (m_Post != null) ? m_Post(request) : await base.OnPostRequest(request);
        protected override async Task<Response> OnPutRequest(Request request) => (m_Put != null) ? m_Put(request) : await base.OnPutRequest(request);
        protected override async Task<Response> OnTraceRequest(Request request) => (m_Trace != null) ? m_Trace(request) : await base.OnTraceRequest(request);
    }
}
