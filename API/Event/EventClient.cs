using CorpseLib.DataNotation;
using CorpseLib.Json;
using CorpseLib.Network.WebSocket;

namespace CorpseLib.Network.API.Event
{
    public class EventClient : AWebSocketProtocol
    {
        private class EventAPIClientProtocol(EventClient client) : AWebSocketProtocol
        {
            private readonly EventClient m_Client = client;

            public override async Task OnClose(int status, string message) { }

            public override async Task OnError(Exception ex) { }

            public override async Task OnOpen() { }

            public override async Task OnMessageReceived(string message)
            {
                DataObject json = JsonParser.Parse(message);
                if (json.TryGet("type", out string? type))
                {
                    switch (type)
                    {
                        case "subscribed":
                        {
                            if (json.TryGet("event", out string? @event))
                                m_Client.Subsribed(@event!);
                            break;
                        }
                        case "unsubscribed":
                        {
                            if (json.TryGet("event", out string? @event))
                                m_Client.Unsubsribed(@event!);
                            break;
                        }
                        case "event":
                        {
                            DataNode? node = json.Get("data");
                            if (node != null && json.TryGet("event", out string? @event))
                                await m_Client.Receive(@event!, node);
                            break;
                        }
                    }
                }
            }
        }

        public class EventArgs(string eventType, DataNode data)
        {
            private readonly DataNode m_Data = data;
            private readonly string m_EventType = eventType;

            public string EventType => m_EventType;
            public DataNode Data => m_Data;

            public T? GetData<T>()
            {
                DataHelper.Cast(m_Data, out T? ret);
                return ret;
            }
        }

        private abstract class IEventCanalWrapper
        {
            public virtual async Task Emit(DataNode data) { }
        }

        private class EventCanalWrapper(Canal canal) : IEventCanalWrapper
        {
            private readonly Canal m_Canal = canal;

            public override async Task Emit(DataNode data) => await m_Canal.Trigger();
        }

        private class EventCanalWrapper<T>(Canal<T> canal) : IEventCanalWrapper
        {
            private readonly Canal<T> m_Canal = canal;

            public override async Task Emit(DataNode data)
            {
                if (DataHelper.Cast(data, out T? @event) && @event != null)
                    await m_Canal.Emit(@event);
            }
        }

        private readonly Dictionary<string, IEventCanalWrapper> m_AwaitingWrapper = [];
        private readonly Dictionary<string, IEventCanalWrapper> m_CanalManager = [];

        public static async Task<EventClient?> NewClient(string host, int port, bool isSecured = false)
        {
            EventClient eventClient = new();
            WebSocketClient? webSocket = await WebSocketClient.Connect(URI.Build(isSecured ? "wss" : "ws").Host(host).Port(port).Build(), eventClient);
            if (webSocket == null)
                return null;
            return eventClient;
        }

        public static async Task<EventClient?> NewClient(string host, int port, string path, bool isSecured = false)
        {
            EventClient eventClient = new();
            WebSocketClient? webSocket = await WebSocketClient.Connect(URI.Build(isSecured ? "wss" : "ws").Host(host).Port(port).Path(path).Build(), eventClient);
            if (webSocket == null)
                return null;
            return eventClient;
        }

        public override async Task OnMessageReceived(string message)
        {
            DataObject json = JsonParser.Parse(message);
            if (json.TryGet("type", out string? type) && json.TryGet("data", out DataObject? data))
            {
                switch (type)
                {
                    case "error":
                    {
                        if (data!.TryGet("event", out string? @event))
                            m_AwaitingWrapper.Remove(@event!);
                        break;
                    }
                    case "subscribed":
                    {
                        if (data!.TryGet("event", out string? @event))
                            Subsribed(@event!);
                        break;
                    }
                    case "unsubscribed":
                    {
                        if (data!.TryGet("event", out string? @event))
                            Unsubsribed(@event!);
                        break;
                    }
                    case "event":
                    {
                        DataNode? node = data!.Get("data");
                        if (node != null && data!.TryGet("event", out string? @event))
                            await Receive(@event!, node);
                        break;
                    }
                }
            }
        }

        internal async Task Receive(string eventType, DataNode data)
        {
            if (m_CanalManager.TryGetValue(eventType, out IEventCanalWrapper? canalWrapper))
                await canalWrapper.Emit(data);
        }

        internal void Subsribed(string eventType)
        {
            if (m_AwaitingWrapper.TryGetValue(eventType, out IEventCanalWrapper? wrapper))
            {
                m_CanalManager[eventType] = wrapper;
                m_AwaitingWrapper.Remove(eventType);
            }
        }

        private async Task Subscribe(string eventType, IEventCanalWrapper wrapper)
        {
            m_AwaitingWrapper[eventType] = wrapper;
            await Send(JsonParser.NetStr(new DataObject() { { "request", "subscribe" }, { "event", eventType } }));
        }

        public async Task Subscribe(Canal canal, string eventType) => await Subscribe(eventType, new EventCanalWrapper(canal));
        public async Task Subscribe<T>(Canal<T> canal, string eventType) => await Subscribe(eventType, new EventCanalWrapper<T>(canal));

        private void Unsubsribed(string eventType) => m_CanalManager.Remove(eventType);
        public async Task Unubscribe(string eventType) => await Send(JsonParser.NetStr(new DataObject() { { "request", "unsubscribe" }, { "event", eventType } }));

        public override async Task OnClose(int status, string message) { }
        public override async Task OnError(Exception ex) { }
        public override async Task OnOpen() { }
    }
}
