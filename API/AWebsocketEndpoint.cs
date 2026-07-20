using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API
{
    public abstract class AWebsocketEndpoint : AEndpoint
    {
        protected AWebsocketEndpoint(Path path) : base(path) { }
        protected AWebsocketEndpoint(Path path, bool needExactPath) : base(path, needExactPath) { }

        internal async Task RegisterClient(WebsocketReference wsReference) => await OnClientRegistered(wsReference);
        protected virtual async Task OnClientRegistered(WebsocketReference wsReference) { }
        internal async Task ClientMessage(WebsocketReference wsReference, string message) => await OnClientMessage(wsReference, message);
        protected virtual async Task OnClientMessage(WebsocketReference wsReference, string message) { }
        internal async Task ClientUnregistered(WebsocketReference wsReference) => await OnClientUnregistered(wsReference);
        protected virtual async Task OnClientUnregistered(WebsocketReference wsReference) { }
    }
}
