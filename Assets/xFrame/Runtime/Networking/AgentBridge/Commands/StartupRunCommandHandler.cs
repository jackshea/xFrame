using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;
using xFrame.Runtime.Startup;

namespace xFrame.Runtime.Networking.AgentBridge.Commands
{
    /// <summary>
    /// 触发启动流程执行。
    /// </summary>
    public sealed class StartupRunCommandHandler : IAgentRpcCommandHandler
    {
        private readonly IStartupOrchestrator _orchestrator;
        private readonly IStartupProfileProvider _profileProvider;
        private readonly Func<bool> _isPlayingProvider;

        public StartupRunCommandHandler(
            IStartupOrchestrator orchestrator = null,
            IStartupProfileProvider profileProvider = null,
            Func<bool> isPlayingProvider = null)
        {
            _orchestrator = orchestrator ?? StartupOrchestratorHost.GetOrCreate();
            _profileProvider = profileProvider ?? CodeStartupProfileProvider.Default;
            _isPlayingProvider = isPlayingProvider ?? (() => Application.isPlaying);
        }

        public string Method => "startup.run";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            JObject paramObj;
            if (request.Params == null || request.Params.Type == JTokenType.Null)
            {
                paramObj = new JObject();
            }
            else if (request.Params is JObject obj)
            {
                paramObj = obj;
            }
            else
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");
            }

            if (!TryResolveEnvironment(paramObj, out var environment))
            {
                return AgentRpcExecutionResult.Failure(
                    AgentRpcErrorCodes.InvalidParams,
                    "environment must be BootEnvironment name or value.");
            }

            if (!_isPlayingProvider.Invoke())
            {
                var profile = _profileProvider.GetProfile(environment);
                var blockedTasks = StartupTaskConstraints.GetHeadlessBlockedTasks(profile);
                if (blockedTasks.Count > 0)
                {
                    return AgentRpcExecutionResult.Failure(
                        AgentRpcErrorCodes.InvalidParams,
                        $"current mode is not PlayMode, blocked tasks: {string.Join(", ", blockedTasks)}");
                }
            }

            try
            {
                var result = _orchestrator.RunAsync(environment, CancellationToken.None).GetAwaiter().GetResult();
                return AgentRpcExecutionResult.Success(new
                {
                    started = true,
                    success = result.IsSuccess,
                    cancelled = result.IsCancelled,
                    failedTask = result.FailedTaskName,
                    environment = environment.ToString()
                });
            }
            catch (InvalidOperationException ex)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidRequest, ex.Message);
            }
            catch (Exception ex)
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InternalError, "startup.run failed.", ex.Message);
            }
        }

        private static bool TryResolveEnvironment(JObject paramObj, out BootEnvironment environment)
        {
            environment = BootEnvironment.DevFull;
            if (paramObj == null)
            {
                return true;
            }

            var environmentToken = paramObj["environment"];
            if (environmentToken == null || environmentToken.Type == JTokenType.Null)
            {
                return true;
            }

            if (environmentToken.Type == JTokenType.Integer)
            {
                var environmentValue = environmentToken.Value<int>();
                if (!Enum.IsDefined(typeof(BootEnvironment), environmentValue))
                {
                    return false;
                }

                environment = (BootEnvironment)environmentValue;
                return true;
            }

            var environmentRaw = environmentToken.Value<string>();
            if (!Enum.TryParse(environmentRaw, true, out BootEnvironment parsedEnvironment))
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(BootEnvironment), parsedEnvironment))
            {
                return false;
            }

            environment = parsedEnvironment;
            return true;
        }
    }
}
