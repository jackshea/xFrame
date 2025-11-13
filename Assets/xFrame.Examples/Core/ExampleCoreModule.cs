using UnityEngine;
using xFrame.Core;
using xFrame.Core.Logging;

namespace xFrame.Examples.Core
{
    /// <summary>
    /// 示例核心模块
    /// 展示如何创建和使用自定义模块
    /// </summary>
    public class ExampleCoreModule : BaseModule
    {
        public override string ModuleName => "ExampleCoreModule";

        /// <summary>
        /// 模块优先级
        /// 数值越小，优先级越高
        /// </summary>
        public override int Priority => 100;

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly IXLogger _logger;

        /// <summary>
        /// 运行时间计数器
        /// </summary>
        private float _runningTime = 0f;

        /// <summary>
        /// 日志输出间隔（秒）
        /// </summary>
        private const float LOG_INTERVAL = 5f;

        /// <summary>
        /// 上次日志输出时间
        /// </summary>
        private float _lastLogTime = 0f;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logManager">日志管理器</param>
        public ExampleCoreModule(IXLogManager logManager)
        {
            _logger = logManager.GetLogger(GetType());
        }

        /// <summary>
        /// 模块初始化
        /// </summary>
        public override void OnInit()
        {
            _logger.Info("ExampleCoreModule 正在初始化...");
            
            // 在这里执行模块初始化逻辑
            
            _logger.Info("ExampleCoreModule 初始化完成");
        }

        /// <summary>
        /// 模块启动
        /// </summary>
        public override void OnStart()
        {
            _logger.Info("ExampleCoreModule 正在启动...");
            
            // 在这里执行模块启动逻辑
            _runningTime = 0f;
            _lastLogTime = 0f;
            
            _logger.Info("ExampleCoreModule 启动完成");
        }

        /// <summary>
        /// 模块更新
        /// 每帧调用
        /// </summary>
        public void OnUpdate()
        {
            // 更新运行时间
            _runningTime += Time.deltaTime;
            
            // 每隔一段时间输出一次日志
            if (_runningTime - _lastLogTime >= LOG_INTERVAL)
            {
                _logger.Info($"ExampleCoreModule 已运行 {_runningTime:F1} 秒");
                _lastLogTime = _runningTime;
            }
            
            // 在这里执行模块更新逻辑
        }

        /// <summary>
        /// 模块销毁
        /// </summary>
        public override void OnDestroy()
        {
            _logger.Info("ExampleCoreModule 正在销毁...");
            
            // 在这里执行模块清理逻辑
            
            _logger.Info("ExampleCoreModule 销毁完成");
        }
    }
}
