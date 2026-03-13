using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using xFrame.Runtime.Startup;
using xFrame.Runtime.Unity.Startup;

namespace xFrame.Tests.PlayMode
{
    /// <summary>
    ///     Unity 启动入口的 PlayMode 回归测试。
    ///     目标是验证 Unity 薄层是否能在真实帧循环里正确触发纯 C# 启动运行时。
    /// </summary>
    [TestFixture]
    public class StartupEntryPlayModeTests
    {
        private GameObject _entryRoot;

        /// <summary>
        ///     清理测试期间创建的对象，避免影响后续用例。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_entryRoot != null)
            {
                Object.Destroy(_entryRoot);
                yield return null;
                _entryRoot = null;
            }
        }

        /// <summary>
        ///     验证自动启动会在进入帧循环后自动执行启动任务并发布成功状态。
        /// </summary>
        [UnityTest]
        public IEnumerator Start_WithAutoRun_ShouldExecuteStartupAutomatically()
        {
            var installer = CreateEntry(
                TestTaskMode.Success,
                BootEnvironment.DevFull,
                out var view,
                out var loadingRoot,
                out _);

            yield return null;
            yield return null;

            var lifecycleState = ResolveLifecycleState();
            Assert.Greater(installer.ExecuteCount, 0, "自动启动后应至少执行一次启动任务");
            Assert.AreEqual(StartupLifecycleStage.Succeeded, lifecycleState.Current.Stage, "启动完成后应发布成功状态");
            Assert.IsFalse(loadingRoot.activeSelf, "成功后应关闭加载节点");
            Assert.AreEqual("正在执行: EnterLobby", view.LastMessage, "成功链路应走到配置的最后一个启动任务");
        }

        /// <summary>
        ///     验证跳战斗环境会走测试战斗分支，而不是 Lobby 分支。
        /// </summary>
        [UnityTest]
        public IEnumerator Start_WithSkipToBattleEnvironment_ShouldRunBattleProfile()
        {
            var installer = CreateEntry(
                TestTaskMode.Success,
                BootEnvironment.DevSkipToBattle,
                out var view,
                out _,
                out _);

            yield return null;
            yield return null;

            var lifecycleState = ResolveLifecycleState();
            Assert.AreEqual(StartupLifecycleStage.Succeeded, lifecycleState.Current.Stage, "跳战斗环境应完成启动");
            Assert.IsTrue(installer.WasExecuted(StartupTaskKey.MockLogin), "跳战斗环境应执行模拟登录");
            Assert.IsTrue(installer.WasExecuted(StartupTaskKey.LoadTestBattleScene), "跳战斗环境应执行测试战斗加载");
            Assert.IsFalse(installer.WasExecuted(StartupTaskKey.EnterLobby), "跳战斗环境不应进入 Lobby");
            Assert.AreEqual("正在执行: LoadTestBattleScene", view.LastMessage, "最后一个执行任务应为测试战斗加载");
        }

        /// <summary>
        ///     验证启动失败会通过统一失败出口驱动 Unity 视图展示错误。
        /// </summary>
        [UnityTest]
        public IEnumerator Start_WhenTaskFails_ShouldPresentErrorThroughUnityView()
        {
            LogAssert.Expect(LogType.Error, "[Startup] 测试失败: CheckUpdate");
            LogAssert.Expect(LogType.Error, "[Startup] 测试失败: CheckUpdate");
            CreateEntry(
                TestTaskMode.FailOnCheckUpdate,
                BootEnvironment.DevFull,
                out var view,
                out _,
                out var errorRoot);

            yield return null;
            yield return null;

            var lifecycleState = ResolveLifecycleState();
            Assert.AreEqual(StartupLifecycleStage.Failed, lifecycleState.Current.Stage, "失败后应发布失败状态");
            Assert.IsTrue(errorRoot.activeSelf, "失败后应激活错误节点");
            StringAssert.Contains("测试失败", view.LastMessage, "统一失败出口应将错误文案交给 Unity 视图");
            Assert.AreEqual("CheckUpdate", lifecycleState.Current.PipelineResult.FailedTaskName, "失败任务名应与测试配置一致");
        }

        /// <summary>
        ///     创建一组可复用的 Unity 启动入口测试对象。
        /// </summary>
        private TestStartupInstaller CreateEntry(
            TestTaskMode taskMode,
            BootEnvironment environment,
            out UnityStartupView view,
            out GameObject loadingRoot,
            out GameObject errorRoot)
        {
            _entryRoot = new GameObject("StartupEntryPlayModeTestRoot");
            _entryRoot.SetActive(false);

            loadingRoot = new GameObject("LoadingRoot");
            loadingRoot.transform.SetParent(_entryRoot.transform, false);
            loadingRoot.SetActive(false);

            errorRoot = new GameObject("ErrorRoot");
            errorRoot.transform.SetParent(_entryRoot.transform, false);
            errorRoot.SetActive(false);

            view = _entryRoot.AddComponent<UnityStartupView>();
            SetPrivateField(view, "_loadingRoot", loadingRoot);
            SetPrivateField(view, "_errorRoot", errorRoot);

            var installer = _entryRoot.AddComponent<TestStartupInstaller>();
            installer.TaskMode = taskMode;

            var entry = _entryRoot.AddComponent<UnityStartupEntry>();
            SetPrivateField(entry, "_environment", environment);
            SetPrivateField(entry, "_autoRunOnStart", true);
            SetPrivateField(entry, "_dontDestroyScopeOnLoad", false);
            SetPrivateField(entry, "_view", view);
            SetPrivateField(entry, "_installer", installer);

            _entryRoot.SetActive(true);
            return installer;
        }

        /// <summary>
        ///     从当前启动入口的容器中读取生命周期状态。
        /// </summary>
        private IStartupLifecycleState ResolveLifecycleState()
        {
            var entry = _entryRoot.GetComponent<UnityStartupEntry>();
            Assert.IsNotNull(entry, "测试入口应已创建");
            Assert.IsNotNull(entry.LifetimeScope, "启动入口应创建默认 LifetimeScope");
            return entry.LifetimeScope.Container.Resolve<IStartupLifecycleState>();
        }

        /// <summary>
        ///     设置组件的私有序列化字段，避免为测试放宽运行时代码可见性。
        /// </summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"未找到字段: {fieldName}");
            field.SetValue(target, value);
        }

        /// <summary>
        ///     启动任务行为模式。
        /// </summary>
        private enum TestTaskMode
        {
            Success = 0,
            FailOnCheckUpdate = 1
        }

        /// <summary>
        ///     PlayMode 测试专用安装器。
        ///     为所有内置任务键注册可预测的测试任务，避免依赖业务初始化。
        /// </summary>
        private sealed class TestStartupInstaller : UnityStartupInstallerBase
        {
            private readonly System.Collections.Generic.List<StartupTaskKey> _executedTaskKeys = new();

            public int ExecuteCount { get; private set; }

            public TestTaskMode TaskMode { get; set; }

            /// <summary>
            ///     判断指定任务是否在当前用例中执行过。
            /// </summary>
            public bool WasExecuted(StartupTaskKey taskKey)
            {
                return _executedTaskKeys.Contains(taskKey);
            }

            /// <summary>
            ///     注册当前用例所需的测试任务。
            /// </summary>
            public override void Install(StartupTaskRegistry registry)
            {
                Register(registry, StartupTaskKey.InitLogger);
                Register(registry, StartupTaskKey.LoadLocalConfig);
                Register(registry, StartupTaskKey.CheckUpdate);
                Register(registry, StartupTaskKey.SdkInit);
                Register(registry, StartupTaskKey.NetworkConnect);
                Register(registry, StartupTaskKey.MockLogin);
                Register(registry, StartupTaskKey.LoadTestBattleScene);
                Register(registry, StartupTaskKey.EnterLobby);
            }

            /// <summary>
            ///     为指定任务键注册测试任务。
            /// </summary>
            private void Register(StartupTaskRegistry registry, StartupTaskKey taskKey)
            {
                registry.Register(taskKey, _ => new DelegateStartupTask(
                    taskKey.ToString(),
                    1f,
                    (_, cancellationToken) => ExecuteAsync(taskKey, cancellationToken)));
            }

            /// <summary>
            ///     执行测试任务，并按配置决定成功或失败。
            /// </summary>
            private Task<StartupTaskResult> ExecuteAsync(StartupTaskKey taskKey, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteCount++;
                _executedTaskKeys.Add(taskKey);

                if (TaskMode == TestTaskMode.FailOnCheckUpdate && taskKey == StartupTaskKey.CheckUpdate)
                    return Task.FromResult(StartupTaskResult.Failed("TestFailure", "测试失败: CheckUpdate", true));

                return Task.FromResult(StartupTaskResult.Success());
            }
        }
    }
}
