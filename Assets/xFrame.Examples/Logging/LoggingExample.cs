using System;
using System.Threading;
using UnityEngine;
using VContainer;
using xFrame.Runtime.Logging;
using Random = UnityEngine.Random;

namespace xFrame.Examples.Logging
{
    /// <summary>
    /// 日志系统使用示例
    /// 演示如何在项目中使用日志系统的各种功能
    /// </summary>
    public class LoggingExample : MonoBehaviour
    {
        private IXLogger _logger;

        [Inject]
        private IXLogManager _logManager;

        /// <summary>
        /// Unity Start生命周期方法
        /// </summary>
        private void Start()
        {
            // 获取当前类的日志记录器
            _logger = _logManager.GetLogger<LoggingExample>();

            // 初始化静态日志访问接口
            XLog.Initialize(_logManager);

            // 演示各种日志功能
            DemonstrateBasicLogging();
            DemonstrateStaticLogging();
            DemonstrateExceptionLogging();
            DemonstrateConditionalLogging();
        }

        /// <summary>
        /// 演示基本日志功能
        /// </summary>
        private void DemonstrateBasicLogging()
        {
            _logger.Info("=== 基本日志功能演示 ===");

            // 不同等级的日志
            _logger.Verbose("这是一条详细日志，通常用于最详细的调试信息");
            _logger.Debug("这是一条调试日志，用于开发时的调试信息");
            _logger.Info("这是一条信息日志，记录程序的正常运行信息");
            _logger.Warning("这是一条警告日志，表示可能存在的问题");
            _logger.Error("这是一条错误日志，表示程序出现了错误");
            _logger.Fatal("这是一条致命日志，表示严重的系统错误");
        }

        /// <summary>
        /// 演示静态日志访问
        /// </summary>
        private void DemonstrateStaticLogging()
        {
            XLog.Info("=== 静态日志访问演示 ===", "StaticExample");

            // 使用静态方法记录日志
            XLog.Debug("使用静态方法记录调试日志", "StaticExample");
            XLog.Info("使用静态方法记录信息日志", "StaticExample");
            XLog.Warning("使用静态方法记录警告日志", "StaticExample");

            // 获取特定类型的Logger
            var specificLogger = XLog.GetLogger<LoggingExample>();
            specificLogger.Info("通过静态接口获取的特定类型Logger");
        }

        /// <summary>
        /// 演示异常日志记录
        /// </summary>
        private void DemonstrateExceptionLogging()
        {
            _logger.Info("=== 异常日志记录演示 ===");

            try
            {
                // 故意制造一个异常
                throw new InvalidOperationException("这是一个演示异常");
            }
            catch (Exception ex)
            {
                // 记录带异常信息的错误日志
                _logger.Error("捕获到异常", ex);
                XLog.Error("使用静态方法记录异常", ex, "ExceptionExample");
            }
        }

        /// <summary>
        /// 演示条件日志记录
        /// </summary>
        private void DemonstrateConditionalLogging()
        {
            _logger.Info("=== 条件日志记录演示 ===");

            // 检查日志等级是否启用，避免不必要的字符串构造
            if (_logger.IsLevelEnabled(LogLevel.Debug))
            {
                var expensiveDebugInfo = GenerateExpensiveDebugInfo();
                _logger.Debug($"调试信息: {expensiveDebugInfo}");
            }

            // 演示日志等级过滤
            _logger.MinLevel = LogLevel.Warning;
            _logger.Debug("这条调试日志不会被输出，因为最小等级设置为Warning");
            _logger.Warning("这条警告日志会被输出");

            // 恢复日志等级
            _logger.MinLevel = LogLevel.Debug;
        }

        /// <summary>
        /// 生成昂贵的调试信息（模拟）
        /// </summary>
        /// <returns>调试信息字符串</returns>
        private string GenerateExpensiveDebugInfo()
        {
            // 模拟一个计算成本较高的操作
            Thread.Sleep(10);
            return $"当前时间: {DateTime.Now}, 随机数: {Random.Range(1, 1000)}";
        }

        /// <summary>
        /// Unity按钮事件：测试不同日志等级
        /// </summary>
        [ContextMenu("测试所有日志等级")]
        public void TestAllLogLevels()
        {
            _logger.Verbose("Verbose级别日志测试");
            _logger.Debug("Debug级别日志测试");
            _logger.Info("Info级别日志测试");
            _logger.Warning("Warning级别日志测试");
            _logger.Error("Error级别日志测试");
            _logger.Fatal("Fatal级别日志测试");
        }

        /// <summary>
        /// Unity按钮事件：测试异常日志
        /// </summary>
        [ContextMenu("测试异常日志")]
        public void TestExceptionLogging()
        {
            try
            {
                var z = 0;
                var result = 10 / z; // 故意制造除零异常
            }
            catch (Exception ex)
            {
                _logger.Fatal("发生了除零异常", ex);
            }
        }

        /// <summary>
        /// Unity按钮事件：刷新所有日志缓冲区
        /// </summary>
        [ContextMenu("刷新日志缓冲区")]
        public void FlushAllLogs()
        {
            XLog.FlushAll();
            _logger.Info("已刷新所有日志缓冲区");
        }
    }
}