namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    /// RPC 命令处理器接口。
    /// </summary>
    public interface IAgentRpcCommandHandler
    {
        /// <summary>
        /// 方法名。
        /// </summary>
        string Method { get; }

        /// <summary>
        /// 是否要求先认证。
        /// </summary>
        bool RequiresAuthentication { get; }

        /// <summary>
        /// 执行命令。
        /// </summary>
        AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context);
    }
}
