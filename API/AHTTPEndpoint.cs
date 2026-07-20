using CorpseLib.Network.Http;
using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API
{
    public abstract class AHTTPEndpoint : AEndpoint
    {
        protected AHTTPEndpoint(Path path) : base(path) { }
        protected AHTTPEndpoint(Path path, bool needExactPath) : base(path, needExactPath) { }

        internal async Task<Response> HandleRequest(Request request) => await OnRequest(request);

        protected virtual async Task<Response> OnRequest(Request request) => request.Method switch
        {
            Request.MethodType.GET => await OnGetRequest(request),
            Request.MethodType.HEAD => await OnHeadRequest(request),
            Request.MethodType.POST => await OnPostRequest(request),
            Request.MethodType.PUT => await OnPutRequest(request),
            Request.MethodType.DELETE => await OnDeleteRequest(request),
            Request.MethodType.CONNECT => await OnConnectRequest(request),
            Request.MethodType.OPTIONS => await OnOptionsRequest(request),
            Request.MethodType.TRACE => await OnTraceRequest(request),
            Request.MethodType.PATCH => await OnPatchRequest(request),
            _ => new(400, "Bad Request")
        };

        protected virtual async Task<Response> OnGetRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnHeadRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnPostRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnPutRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnDeleteRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnConnectRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnOptionsRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnTraceRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual async Task<Response> OnPatchRequest(Request request) => new(405, "Method Not Allowed");
    }
}
