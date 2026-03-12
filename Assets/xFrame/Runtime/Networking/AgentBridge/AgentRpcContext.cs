using System;
using Newtonsoft.Json;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    ///     单连接上下文。
    /// </summary>
    public sealed class AgentRpcContext
    {
        private Action<string> _notificationSink;

        public AgentRpcContext(string connectionId, AgentBridgeOptions options, AgentCommandRegistry registry,
            Action<string> notificationSink = null)
        {
            ConnectionId = connectionId;
            Options = options;
            Registry = registry;
            IsAuthenticated = options == null || !options.AuthenticationEnabled;
            _notificationSink = notificationSink;
        }

        public string ConnectionId { get; }

        public AgentBridgeOptions Options { get; }

        public AgentCommandRegistry Registry { get; }

        public bool IsAuthenticated { get; set; }

        public void UpdateNotificationSink(Action<string> notificationSink)
        {
            _notificationSink = notificationSink;
        }

        public void Notify(string method, object payload)
        {
            if (_notificationSink == null || string.IsNullOrWhiteSpace(method)) return;

            var notification = new JsonRpcNotification
            {
                Method = method,
                Params = payload
            };

            _notificationSink(JsonConvert.SerializeObject(notification));
        }

        public void PublishEvent(string eventName, object payload)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return;

            Notify("agent.event", new
            {
                name = eventName,
                payload
            });
        }
    }

    public sealed class AgentRpcExecutionResult
    {
        public object Result { get; private set; }

        public JsonRpcError Error { get; private set; }

        public static AgentRpcExecutionResult Success(object result)
        {
            return new AgentRpcExecutionResult { Result = result };
        }

        public static AgentRpcExecutionResult Failure(int code, string message, object data = null)
        {
            return new AgentRpcExecutionResult
            {
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
        }
    }
}
