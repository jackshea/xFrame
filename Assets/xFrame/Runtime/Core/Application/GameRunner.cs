using System;
using System.Collections.Generic;
using xFrame.Runtime.Core.Scheduler;

namespace xFrame.Runtime.Core
{
    /// <summary>
    /// 核心游戏运行器 - 驱动核心层在无Unity环境下运行
    /// </summary>
    public class GameRunner : IGameRunner
    {
        private readonly ITimeProvider _timeProvider;
        private readonly ICoreLogManager _logManager;
        private readonly ICoreScheduler _scheduler;
        private readonly Dictionary<Type, object> _services = new();
        private bool _isRunning;
        private bool _isInitialized;

        public bool IsRunning => _isRunning;
        public IGameCore GameCore { get; private set; }
        public ITimeProvider TimeProvider => _timeProvider;
        public ICoreLogManager LogManager => _logManager;

        public GameRunner(ITimeProvider timeProvider, ICoreLogManager logManager, ICoreScheduler scheduler)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// 注册服务（带接口）
        /// </summary>
        public void RegisterService<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new()
        {
            _services[typeof(TInterface)] = new TImplementation();
        }

        public void Run()
        {
            if (_isRunning)
            {
                _logManager.GetLogger("GameRunner").Warning("GameRunner 已经在运行中");
                return;
            }

            _logManager.GetLogger("GameRunner").Info("=== 游戏核心层启动 ===");
            
            _isRunning = true;
            _isInitialized = true;

            // 初始化所有注册的服务
            InitializeServices();

            _logManager.GetLogger("GameRunner").Info("游戏核心层启动完成");
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _logManager.GetLogger("GameRunner").Info("=== 游戏核心层停止 ===");

            _isRunning = false;
            _scheduler.CancelAll();

            // 触发关闭事件
            GameCore?.Shutdown();

            _logManager.GetLogger("GameRunner").Info("游戏核心层已停止");
        }

        /// <summary>
        /// 更新游戏（每帧调用）
        /// </summary>
        public void Update()
        {
            if (!_isRunning || !_isInitialized)
                return;

            // 更新时间提供者
            _timeProvider.Tick();

            // 更新调度器
            _scheduler.Update(_timeProvider.DeltaTime, _timeProvider.UnscaledDeltaTime);

            // 更新核心层
            GameCore?.Update(_timeProvider.DeltaTime, _timeProvider.UnscaledDeltaTime);
        }

        /// <summary>
        /// 固定更新（物理等）
        /// </summary>
        public void FixedUpdate()
        {
            if (!_isRunning || !_isInitialized)
                return;

            // 可用于固定时间步更新逻辑
        }

        /// <summary>
        /// 延迟更新（相机等）
        /// </summary>
        public void LateUpdate()
        {
            if (!_isRunning || !_isInitialized)
                return;

            // 可用于延迟更新逻辑
        }

        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;
            return null;
        }

        public object GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }

        private void InitializeServices()
        {
            var logger = _logManager.GetLogger("GameRunner");
            logger.Info("初始化核心服务...");
            
            foreach (var service in _services.Values)
            {
                logger.Debug($"初始化服务: {service.GetType().Name}");
            }
            
            logger.Info("核心服务初始化完成");
        }

        /// <summary>
        /// 创建模拟环境运行器（用于测试）
        /// </summary>
        public static GameRunner CreateSimulated(int framesPerSecond = 60)
        {
            var timeProvider = new SimulatedTimeProvider();
            var logManager = new CoreLogManager();
            var scheduler = new CoreScheduler(logManager);

            var runner = new GameRunner(timeProvider, logManager, scheduler);
            
            // 注册默认服务
            runner.RegisterService<ICoreScheduler>(scheduler);

            return runner;
        }
    }
}
