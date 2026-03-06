using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime.Core;
using xFrame.Runtime.Core.Scheduler;
using xFrame.Runtime.Unity.Adapter;

namespace xFrame.Runtime.Unity.Bootstrapper
{
    /// <summary>
    ///     Unity游戏运行器 - 在Unity环境中驱动核心层
    ///     桥接Unity生命周期与核心层
    /// </summary>
    public class UnityGameRunner : MonoBehaviour, IGameRunner
    {
        [Header("核心层配置")] [SerializeField] private bool _autoStart = true;

        [SerializeField] private bool _dontDestroyOnLoad = true;
        private GameRunner _coreRunner;
        private ICoreScheduler _scheduler;

        private void Awake()
        {
            if (_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (_autoStart) Run();
        }

        private void Start()
        {
            // Unity生命周期已准备好
        }

        private void Update()
        {
            if (IsRunning && _coreRunner != null) _coreRunner.Update();
        }

        private void FixedUpdate()
        {
            if (IsRunning && _coreRunner != null) _coreRunner.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (IsRunning && _coreRunner != null) _coreRunner.LateUpdate();
        }

        private void OnDestroy()
        {
            Stop();
        }

        public bool IsRunning { get; private set; }

        public IGameCore GameCore => _coreRunner?.GameCore;
        public ITimeProvider TimeProvider { get; private set; }

        public ICoreLogManager LogManager { get; private set; }

        public void Run()
        {
            if (IsRunning)
            {
                Debug.LogWarning("UnityGameRunner 已经在运行中");
                return;
            }

            // 创建Unity时间提供者
            TimeProvider = new UnityTimeProvider();

            // 创建核心日志管理器（添加Unity输出器）
            LogManager = new CoreLogManager(new ICoreLogAppender[]
            {
                new ConsoleLogAppender(),
                new UnityLogAppender()
            });

            // 创建调度器
            _scheduler = new CoreScheduler(LogManager);

            // 创建核心运行器
            _coreRunner = new GameRunner(TimeProvider, LogManager, _scheduler);

            // 注册核心服务
            _coreRunner.RegisterService(TimeProvider);
            _coreRunner.RegisterService(_scheduler);

            // 启动核心层
            _coreRunner.Run();
            IsRunning = true;

            Debug.Log("UnityGameRunner 已启动");
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            _coreRunner?.Stop();
            IsRunning = false;

            Debug.Log("UnityGameRunner 已停止");
        }

        /// <summary>
        ///     暂停游戏
        /// </summary>
        public void Pause()
        {
            if (TimeProvider != null) TimeProvider.IsPaused = true;
        }

        /// <summary>
        ///     恢复游戏
        /// </summary>
        public void Resume()
        {
            if (TimeProvider != null) TimeProvider.IsPaused = false;
        }

        /// <summary>
        ///     设置时间缩放
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            if (TimeProvider != null) TimeProvider.TimeScale = timeScale;
        }
    }

    /// <summary>
    ///     核心层生命周期作用域 - VContainer集成
    /// </summary>
    public class CoreLifetimeScope : LifetimeScope
    {
        [SerializeField] private bool _autoRun = true;

        protected override void Configure(IContainerBuilder builder)
        {
            // 注册Unity时间提供者
            builder.Register<UnityTimeProvider>(Lifetime.Singleton)
                .As<ITimeProvider>();

            // 注册核心日志管理器
            builder.Register<CoreLogManager>(Lifetime.Singleton)
                .As<ICoreLogManager>();

            // 注册核心调度器
            builder.Register<CoreScheduler>(Lifetime.Singleton)
                .As<ICoreScheduler>();

            // 注册核心运行器
            builder.Register<GameRunner>(Lifetime.Transient);
        }
    }
}