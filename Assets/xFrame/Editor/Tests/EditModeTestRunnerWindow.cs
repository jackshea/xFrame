using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using xFrame.Runtime.Logging;

namespace xFrame.Editor.Tests
{
    /// <summary>
    ///     在已打开的 Unity Editor 内执行 EditMode 测试。
    /// </summary>
    public class EditModeTestRunnerWindow : EditorWindow
    {
        private const string FilterPrefsKey = "xFrame.EditModeTests.Filter";

        private static readonly IXLogger Logger = new XLogManager().GetLogger("EditModeTests");

        private string _testFilter;

        private void OnEnable()
        {
            _testFilter = EditorPrefs.GetString(FilterPrefsKey, string.Empty);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("EditMode Test Runner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("在当前已打开的 Unity Editor 中执行 EditMode 测试。", MessageType.Info);

            EditorGUILayout.Space();
            _testFilter = EditorGUILayout.TextField("Test Filter", _testFilter ?? string.Empty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run All")) RunTests(string.Empty);

            if (GUILayout.Button("Run By Filter")) RunTests(_testFilter);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Filter 示例：", "xFrame.Tests.SchedulerTests.SchedulerServiceTests");
        }

        [MenuItem("xFrame/Tests/EditMode Runner")]
        public static void ShowWindow()
        {
            GetWindow<EditModeTestRunnerWindow>("EditMode Runner");
        }

        private static string GetResultsPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? ".", "Logs", "EditModeResults.xml");
        }

        private void RunTests(string testFilter)
        {
            _testFilter = testFilter ?? string.Empty;
            EditorPrefs.SetString(FilterPrefsKey, _testFilter);

            var filter = new Filter
            {
                testMode = TestMode.EditMode
            };

            if (!string.IsNullOrWhiteSpace(_testFilter)) filter.testNames = new[] { _testFilter };

            var resultsPath = GetResultsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath) ?? "Logs");

            var api = CreateInstance<TestRunnerApi>();
            var callback = new EditModeTestResultCallback(api, resultsPath);
            api.RegisterCallbacks(callback);

            var executionSettings = new ExecutionSettings(filter)
            {
                runSynchronously = true
            };

            Logger.Info($"Start EditMode tests. Filter='{_testFilter ?? string.Empty}'");
            api.Execute(executionSettings);
        }

        private sealed class EditModeTestResultCallback : ICallbacks
        {
            private readonly TestRunnerApi _api;
            private readonly string _resultsPath;

            public EditModeTestResultCallback(TestRunnerApi api, string resultsPath)
            {
                _api = api;
                _resultsPath = resultsPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    var passCount = result?.PassCount ?? 0;
                    var failCount = result?.FailCount ?? 0;
                    var skipCount = result?.SkipCount ?? 0;
                    var inconclusiveCount = result?.InconclusiveCount ?? 0;
                    var totalCount = passCount + failCount + skipCount + inconclusiveCount;
                    WriteSummaryResultFile(
                        _resultsPath,
                        totalCount,
                        passCount,
                        failCount,
                        skipCount,
                        inconclusiveCount);

                    Logger.Info(
                        $"EditMode tests finished. Total={totalCount}, Passed={passCount}, Failed={failCount}, Skipped={skipCount}, Inconclusive={inconclusiveCount}, Results='{_resultsPath}'");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to write EditMode test result file.", ex);
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

            private static void WriteSummaryResultFile(
                string path,
                int totalCount,
                int passCount,
                int failCount,
                int skipCount,
                int inconclusiveCount)
            {
                var xml = new StringBuilder(256);
                xml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                xml.Append("<test-run");
                xml.Append($" total=\"{totalCount}\"");
                xml.Append($" passed=\"{passCount}\"");
                xml.Append($" failed=\"{failCount}\"");
                xml.Append($" skipped=\"{skipCount}\"");
                xml.Append($" inconclusive=\"{inconclusiveCount}\"");
                xml.Append(" />");
                File.WriteAllText(path, xml.ToString());
            }
        }
    }
}