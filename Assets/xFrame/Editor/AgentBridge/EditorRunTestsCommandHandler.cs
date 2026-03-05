using System;
using Newtonsoft.Json.Linq;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    /// 触发 Unity Test Runner 执行测试。
    /// </summary>
    public sealed class EditorRunTestsCommandHandler : IAgentRpcCommandHandler
    {
        private static TestRunSnapshot _lastSnapshot = TestRunSnapshot.Empty;

        public string Method => "unity.tests.run";

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

            var modeRaw = paramObj.Value<string>("mode");
            if (!TryParseMode(modeRaw, out var mode))
            {
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "mode must be EditMode or PlayMode.");
            }

            var filterName = paramObj.Value<string>("filter");
            var runId = Guid.NewGuid().ToString("N");
            var filter = BuildFilter(mode, filterName);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callback = new AgentBridgeTestRunCallback(api, runId, mode, filterName);
            api.RegisterCallbacks(callback);

            _lastSnapshot = new TestRunSnapshot
            {
                RunId = runId,
                Mode = mode.ToString(),
                Filter = filterName,
                Status = "running"
            };

            var settings = new ExecutionSettings(new[] { filter })
            {
                runSynchronously = mode == TestMode.EditMode
            };

            api.Execute(settings);

            if (mode != TestMode.EditMode)
            {
                return AgentRpcExecutionResult.Success(new
                {
                    started = true,
                    completed = false,
                    runId,
                    mode = mode.ToString(),
                    filter = filterName,
                    message = "PlayMode run started. Use unity.tests.lastResult to query completion."
                });
            }

            return AgentRpcExecutionResult.Success(new
            {
                started = true,
                completed = true,
                runId = _lastSnapshot.RunId,
                mode = _lastSnapshot.Mode,
                filter = _lastSnapshot.Filter,
                status = _lastSnapshot.Status,
                summary = _lastSnapshot.Summary,
                failures = _lastSnapshot.Failures
            });
        }

        public sealed class LastResultHandler : IAgentRpcCommandHandler
        {
            public string Method => "unity.tests.lastResult";

            public bool RequiresAuthentication => true;

            public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
            {
                return AgentRpcExecutionResult.Success(new
                {
                    hasResult = !string.IsNullOrWhiteSpace(_lastSnapshot.RunId),
                    runId = _lastSnapshot.RunId,
                    mode = _lastSnapshot.Mode,
                    filter = _lastSnapshot.Filter,
                    status = _lastSnapshot.Status,
                    summary = _lastSnapshot.Summary,
                    failures = _lastSnapshot.Failures
                });
            }
        }

        private static Filter BuildFilter(TestMode mode, string filterName)
        {
            var filter = new Filter
            {
                testMode = mode
            };

            if (!string.IsNullOrWhiteSpace(filterName))
            {
                filter.testNames = new[] { filterName };
            }

            return filter;
        }

        private static bool TryParseMode(string modeRaw, out TestMode mode)
        {
            if (string.IsNullOrWhiteSpace(modeRaw))
            {
                mode = TestMode.EditMode;
                return true;
            }

            if (string.Equals(modeRaw, nameof(TestMode.EditMode), StringComparison.OrdinalIgnoreCase))
            {
                mode = TestMode.EditMode;
                return true;
            }

            if (string.Equals(modeRaw, nameof(TestMode.PlayMode), StringComparison.OrdinalIgnoreCase))
            {
                mode = TestMode.PlayMode;
                return true;
            }

            mode = TestMode.EditMode;
            return false;
        }

        private static void UpdateSnapshot(TestRunSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
        }

        private sealed class AgentBridgeTestRunCallback : ICallbacks
        {
            private readonly TestRunnerApi _api;
            private readonly string _runId;
            private readonly TestMode _mode;
            private readonly string _filter;

            public AgentBridgeTestRunCallback(TestRunnerApi api, string runId, TestMode mode, string filter)
            {
                _api = api;
                _runId = runId;
                _mode = mode;
                _filter = filter;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    UpdateSnapshot(new TestRunSnapshot
                    {
                        RunId = _runId,
                        Mode = _mode.ToString(),
                        Filter = _filter,
                        Status = result == null ? "failed" : (result.FailCount > 0 ? "failed" : "passed"),
                        Summary = new
                        {
                            total = (result?.PassCount ?? 0) + (result?.FailCount ?? 0) + (result?.SkipCount ?? 0) + (result?.InconclusiveCount ?? 0),
                            passed = result?.PassCount ?? 0,
                            failed = result?.FailCount ?? 0,
                            skipped = result?.SkipCount ?? 0,
                            inconclusive = result?.InconclusiveCount ?? 0,
                            duration = result?.Duration ?? 0d
                        },
                        Failures = BuildFailureList(result)
                    });
                }
                finally
                {
                    _api.UnregisterCallbacks(this);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static object[] BuildFailureList(ITestResultAdaptor result)
            {
                if (result == null)
                {
                    return Array.Empty<object>();
                }

                var failures = new System.Collections.Generic.List<object>();
                CollectFailures(result, failures);
                return failures.ToArray();
            }

            private static void CollectFailures(ITestResultAdaptor node, System.Collections.Generic.ICollection<object> failures)
            {
                if (node == null)
                {
                    return;
                }

                if (node.FailCount > 0 && !string.IsNullOrWhiteSpace(node.Name) && !string.IsNullOrWhiteSpace(node.Message))
                {
                    failures.Add(new
                    {
                        name = node.Name,
                        message = node.Message,
                        stackTrace = node.StackTrace
                    });
                }

                if (!node.HasChildren)
                {
                    return;
                }

                foreach (var child in node.Children)
                {
                    CollectFailures(child, failures);
                }
            }
        }

        private struct TestRunSnapshot
        {
            public static readonly TestRunSnapshot Empty = new()
            {
                RunId = string.Empty,
                Mode = string.Empty,
                Filter = string.Empty,
                Status = "none",
                Summary = null,
                Failures = Array.Empty<object>()
            };

            public string RunId;
            public string Mode;
            public string Filter;
            public string Status;
            public object Summary;
            public object[] Failures;
        }
    }
}
