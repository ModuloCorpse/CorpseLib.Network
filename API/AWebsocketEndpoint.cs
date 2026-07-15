using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API
{
    public abstract class AWebsocketEndpoint : AEndpoint
    {
        protected AWebsocketEndpoint(Path path) : base(path) { }
        protected AWebsocketEndpoint(Path path, bool needExactPath) : base(path, needExactPath) { }

        internal void RegisterClient(WebsocketReference wsReference) => OnClientRegistered(wsReference);
        protected virtual void OnClientRegistered(WebsocketReference wsReference) { }
        internal void ClientMessage(WebsocketReference wsReference, string message) => OnClientMessage(wsReference, message);
        protected virtual void OnClientMessage(WebsocketReference wsReference, string message) { }
        internal void ClientUnregistered(WebsocketReference wsReference) => OnClientUnregistered(wsReference);
        protected virtual void OnClientUnregistered(WebsocketReference wsReference) { }
    }
}
