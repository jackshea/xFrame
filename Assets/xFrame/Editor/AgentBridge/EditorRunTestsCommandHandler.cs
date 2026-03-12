using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using xFrame.Editor.Tests;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    ///     触发 Unity Test Runner 执行测试。
    /// </summary>
    public sealed class EditorRunTestsCommandHandler : IAgentRpcCommandHandler
    {
        private const string LastSnapshotSessionKey = "xFrame.AgentBridge.LastTestSnapshot";
        private const double ProgressNotificationIntervalSeconds = 1d;
        private static TestRunSnapshot _lastSnapshot = LoadSnapshot();

        public string Method => "unity.tests.run";

        public bool RequiresAuthentication => true;

        public AgentRpcExecutionResult Execute(JsonRpcRequest request, AgentRpcContext context)
        {
            JObject paramObj;
            if (request.Params == null || request.Params.Type == JTokenType.Null)
                paramObj = new JObject();
            else if (request.Params is JObject obj)
                paramObj = obj;
            else
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams, "params must be object.");

            var modeRaw = paramObj.Value<string>("mode");
            if (!TryParseMode(modeRaw, out var mode))
                return AgentRpcExecutionResult.Failure(AgentRpcErrorCodes.InvalidParams,
                    "mode must be EditMode or PlayMode.");

            var filterName = paramObj.Value<string>("filter");
            var runId = Guid.NewGuid().ToString("N");
            var filter = BuildFilter(mode, filterName);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callback = new AgentBridgeTestRunCallback(api, context, runId, mode, filterName);
            api.RegisterCallbacks(callback);

            MarkRunProgress(runId, mode, filterName, 0, 0, 0, 0, 0, 0, 0d, null);

            var settings = new ExecutionSettings(filter)
            {
                runSynchronously = false
            };

            api.Execute(settings);

            return AgentRpcExecutionResult.Success(new
            {
                started = true,
                completed = false,
                runId,
                mode = mode.ToString(),
                filter = filterName,
                status = "running",
                summary = BuildSummary(0, 0, 0, 0, 0, 0, 0d, null),
                failures = Array.Empty<object>(),
                message = "Test run started. Use unity.tests.lastResult to query progress and completion."
            });
        }

        private static Filter BuildFilter(TestMode mode, string filterName)
        {
            return UnityTestFilterFactory.Create(mode, filterName);
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
            SessionState.SetString(LastSnapshotSessionKey, JsonConvert.SerializeObject(snapshot));
        }

        internal static void ResetSnapshotForTests()
        {
            UpdateSnapshot(TestRunSnapshot.Empty);
        }

        internal static void MarkRunProgress(string runId, TestMode mode, string filter, int total, int completed,
            int passed, int failed, int skipped, int inconclusive, double duration, string currentTest)
        {
            UpdateSnapshot(new TestRunSnapshot
            {
                RunId = runId,
                Mode = mode.ToString(),
                Filter = filter,
                Status = "running",
                Summary = BuildSummary(total, completed, passed, failed, skipped, inconclusive, duration, currentTest),
                Failures = Array.Empty<object>()
            });
        }

        internal static void MarkRunFinished(string runId, TestMode mode, string filter, string status, int total,
            int passed, int failed, int skipped, int inconclusive, double duration, object[] failures)
        {
            UpdateSnapshot(new TestRunSnapshot
            {
                RunId = runId,
                Mode = mode.ToString(),
                Filter = filter,
                Status = status,
                Summary = BuildSummary(total, total, passed, failed, skipped, inconclusive, duration, null),
                Failures = failures ?? Array.Empty<object>()
            });
        }

        private static TestRunSnapshot LoadSnapshot()
        {
            var json = SessionState.GetString(LastSnapshotSessionKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return TestRunSnapshot.Empty;

            try
            {
                return JsonConvert.DeserializeObject<TestRunSnapshot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AgentBridge] 加载上次测试快照失败，将回退为空结果。原因: {ex.Message}");
                return TestRunSnapshot.Empty;
            }
        }

        private static object BuildSummary(int total, int completed, int passed, int failed, int skipped,
            int inconclusive, double duration, string currentTest)
        {
            return new
            {
                total,
                completed,
                passed,
                failed,
                skipped,
                inconclusive,
                duration,
                currentTest
            };
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

        private sealed class AgentBridgeTestRunCallback : ICallbacks
        {
            private readonly TestRunnerApi _api;
            private readonly AgentRpcContext _context;
            private readonly string _filter;
            private readonly TestMode _mode;
            private readonly string _runId;
            private bool _isProgressPumpRegistered;
            private double _lastNotificationTime;
            private int _completedCount;
            private int _failedCount;
            private int _inconclusiveCount;
            private int _passedCount;
            private int _skippedCount;
            private int _totalCount;
            private double _totalDuration;
            private string _currentTest;

            public AgentBridgeTestRunCallback(TestRunnerApi api, AgentRpcContext context, string runId, TestMode mode,
                string filter)
            {
                _api = api;
                _context = context;
                _runId = runId;
                _mode = mode;
                _filter = filter;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _totalCount = Math.Max(0, testsToRun?.TestCaseCount ?? 0);
                _completedCount = 0;
                _passedCount = 0;
                _failedCount = 0;
                _skippedCount = 0;
                _inconclusiveCount = 0;
                _totalDuration = 0d;
                _currentTest = null;

                MarkRunProgress(_runId, _mode, _filter, _totalCount, _completedCount, _passedCount, _failedCount,
                    _skippedCount, _inconclusiveCount, _totalDuration, _currentTest);
                StartProgressPump();
                PublishProgressNotification("started");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    var passed = result?.PassCount ?? _passedCount;
                    var failed = result?.FailCount ?? _failedCount;
                    var skipped = result?.SkipCount ?? _skippedCount;
                    var inconclusive = result?.InconclusiveCount ?? _inconclusiveCount;
                    var total = Math.Max(_totalCount, passed + failed + skipped + inconclusive);
                    var status = result == null ? "failed" : failed > 0 ? "failed" : "passed";
                    var failures = BuildFailureList(result);

                    MarkRunFinished(_runId, _mode, _filter, status, total, passed, failed, skipped, inconclusive,
                        result?.Duration ?? _totalDuration, failures);
                    PublishProgressNotification("completed", status, total, total, passed, failed, skipped,
                        inconclusive, result?.Duration ?? _totalDuration, null, failures);
                }
                finally
                {
                    StopProgressPump();
                    _api.UnregisterCallbacks(this);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
                if (test == null || test.IsSuite) return;

                _currentTest = ResolveTestName(test.FullName, test.Name);
                MarkRunProgress(_runId, _mode, _filter, _totalCount, _completedCount, _passedCount, _failedCount,
                    _skippedCount, _inconclusiveCount, _totalDuration, _currentTest);
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result?.Test == null || result.Test.IsSuite) return;

                _completedCount++;
                _currentTest = ResolveTestName(result.FullName, result.Name);
                _totalDuration += Math.Max(0d, result.Duration);

                var resultState = result.ResultState ?? string.Empty;
                if (resultState.StartsWith("Passed", StringComparison.Ordinal))
                {
                    _passedCount++;
                }
                else if (resultState.StartsWith("Failed", StringComparison.Ordinal))
                {
                    _failedCount++;
                }
                else if (resultState.StartsWith("Skipped", StringComparison.Ordinal))
                {
                    _skippedCount++;
                }
                else if (resultState.StartsWith("Inconclusive", StringComparison.Ordinal))
                {
                    _inconclusiveCount++;
                }

                MarkRunProgress(_runId, _mode, _filter, _totalCount, _completedCount, _passedCount, _failedCount,
                    _skippedCount, _inconclusiveCount, _totalDuration, _currentTest);
            }

            private void OnEditorUpdate()
            {
                var now = EditorApplication.timeSinceStartup;
                if (now - _lastNotificationTime < ProgressNotificationIntervalSeconds) return;

                PublishProgressNotification("progress");
            }

            private void StartProgressPump()
            {
                if (_isProgressPumpRegistered) return;

                _lastNotificationTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += OnEditorUpdate;
                _isProgressPumpRegistered = true;
            }

            private void StopProgressPump()
            {
                if (!_isProgressPumpRegistered) return;

                EditorApplication.update -= OnEditorUpdate;
                _isProgressPumpRegistered = false;
            }

            private void PublishProgressNotification(string eventName)
            {
                PublishProgressNotification(eventName, "running", _totalCount, _completedCount, _passedCount,
                    _failedCount, _skippedCount, _inconclusiveCount, _totalDuration, _currentTest,
                    Array.Empty<object>());
            }

            private void PublishProgressNotification(string eventName, string status, int total, int completed,
                int passed, int failed, int skipped, int inconclusive, double duration, string currentTest,
                object[] failures)
            {
                _lastNotificationTime = EditorApplication.timeSinceStartup;
                _context?.PublishEvent("unity.tests.progress", new
                {
                    @event = eventName,
                    runId = _runId,
                    mode = _mode.ToString(),
                    filter = _filter,
                    status,
                    summary = BuildSummary(total, completed, passed, failed, skipped, inconclusive, duration,
                        currentTest),
                    failures = failures ?? Array.Empty<object>()
                });
            }

            private static string ResolveTestName(string fullName, string name)
            {
                return string.IsNullOrWhiteSpace(fullName) ? name : fullName;
            }

            private static object[] BuildFailureList(ITestResultAdaptor result)
            {
                if (result == null) return Array.Empty<object>();

                var failures = new List<object>();
                CollectFailures(result, failures);
                return failures.ToArray();
            }

            private static void CollectFailures(ITestResultAdaptor node, ICollection<object> failures)
            {
                if (node == null) return;

                if (node.FailCount > 0 && !string.IsNullOrWhiteSpace(node.Name) &&
                    !string.IsNullOrWhiteSpace(node.Message))
                    failures.Add(new
                    {
                        name = node.Name,
                        message = node.Message,
                        stackTrace = node.StackTrace
                    });

                if (!node.HasChildren) return;

                foreach (var child in node.Children) CollectFailures(child, failures);
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
