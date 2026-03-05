using System;
using System.Threading;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    /// <summary>
    ///     触发启动流程关闭。
    /// </summary>
    public sealed class StartupStopCommandHandler : IAgentRpcCommandHandler
    {
        private readonly IStartupOrchestrator _orchestrator;

        public StartupStopCommandHandler(IStartupOrchestrator orchestrator = null)
        {
            _orchestrator = orchestrator ?? StartupOrchestratorHost.GetOrCreate();
        }

        public string Method => "startup.stop";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            try
            {
                _orchestrator.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
                return AgentRpcExecutionResult.Success(new
                {
                    stopped = true
                });
            }
            catch (Exception ex)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InternalError, "startup.stop failed.",
                    ex.Message);
            }
        }
    }
}