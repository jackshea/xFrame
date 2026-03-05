namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    ///     单连接上下文。
    /// </summary>
    public sealed class AgentRpcContext
    {
        public AgentRpcContext(string connectionId, AgentBridgeOptions options, AgentCommandRegistry registry)
        {
            ConnectionId = connectionId;
            Options = options;
            Registry = registry;
        }

        public string ConnectionId { get; }

        public AgentBridgeOptions Options { get; }

        public AgentCommandRegistry Registry { get; }

        public bool IsAuthenticated { get; set; }
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