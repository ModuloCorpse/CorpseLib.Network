using CorpseLib.Network.Http;
using Path = CorpseLib.Network.Http.Path;

namespace CorpseLib.Network.API.Event
{
    public class EventUnregisterEndpoint(Path path, EventEndpoint eventEndpoint) : AHTTPEndpoint(path, false)
    {
        private readonly EventEndpoint m_EventEndpoint = eventEndpoint;

        protected override Response OnPostRequest(Request request)
        {
            string id = request.Body;
            if (!string.IsNullOrEmpty(id))
            {
                OperationResult result = m_EventEndpoint.UnregisterClient(id, request.Path[^1]);
                if (result)
                    return new(200, "Ok", "Websocket unregistered");
                return new(400, "Bad Request", result.Description);
            }
            return new(400, "Bad Request", "No websocket given");
        }
    }
}
