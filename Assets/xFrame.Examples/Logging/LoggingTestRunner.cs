using System.Collections;
using UnityEngine;
using VContainer;
using xFrame.Core.Logging;

namespace xFrame.Examples.Logging
{
    /// <summary>
    /// 日志系统测试运行器
    /// 用于自动化测试日志系统的各项功能
    /// </summary>
    public class LoggingTestRunner : MonoBehaviour
    {
        [Inject] private IXLogManager _logManager;
        private IXLogger _logger;

        [Header("测试配置")]
        [SerializeField] private bool autoRunTests = true;
        [SerializeField] private float testInterval = 2f;
        [SerializeField] private int testIterations = 5;

        /// <summary>
        /// Unity Start生命周期方法
        /// </summary>
        private void Start()
        {
            _logger = _logManager.GetLogger<LoggingTestRunner>();
            
            if (autoRunTests)
            {
                StartCoroutine(RunAutomatedTests());
            }
        }

        /// <summary>
        /// 运行自动化测试
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator RunAutomatedTests()
        {
            _logger.Info("开始运行日志系统自动化测试");

            for (int i = 0; i < testIterations; i++)
            {
                _logger.Info($"=== 测试迭代 {i + 1}/{testIterations} ===");
                
                // 测试基本日志功能
                yield return StartCoroutine(TestBasicLogging());
                
                // 测试性能
                yield return StartCoroutine(TestLoggingPerformance());
                
                // 测试异常处理
                yield return StartCoroutine(TestExceptionHandling());
                
                // 测试线程安全（模拟）
                yield return StartCoroutine(TestThreadSafety());
                
                yield return new WaitForSeconds(testInterval);
            }

            _logger.Info("自动化测试完成");
            
            // 最终刷新所有日志
            XLog.FlushAll();
        }

        /// <summary>
        /// 测试基本日志功能
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator TestBasicLogging()
        {
            _logger.Debug("测试基本日志功能开始");
            
            var testLogger = _logManager.GetLogger("TestModule");
            
            testLogger.Verbose("测试Verbose级别日志");
            testLogger.Debug("测试Debug级别日志");
            testLogger.Info("测试Info级别日志");
            testLogger.Warning("测试Warning级别日志");
            testLogger.Error("测试Error级别日志");
            testLogger.Fatal("测试Fatal级别日志");
            
            _logger.Debug("基本日志功能测试完成");
            yield return null;
        }

        /// <summary>
        /// 测试日志性能
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator TestLoggingPerformance()
        {
            _logger.Debug("测试日志性能开始");
            
            var performanceLogger = _logManager.GetLogger("PerformanceTest");
            var startTime = Time.realtimeSinceStartup;
            
            // 快速记录大量日志
            for (int i = 0; i < 1000; i++)
            {
                performanceLogger.Debug($"性能测试日志 #{i}");
                
                // 每100条日志让出一帧
                if (i % 100 == 0)
                {
                    yield return null;
                }
            }
            
            var endTime = Time.realtimeSinceStartup;
            var duration = endTime - startTime;
            
            _logger.Info($"性能测试完成：记录1000条日志耗时 {duration:F3} 秒");
            yield return null;
        }

        /// <summary>
        /// 测试异常处理
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator TestExceptionHandling()
        {
            _logger.Debug("测试异常处理开始");
            
            var exceptionLogger = _logManager.GetLogger("ExceptionTest");
            
            try
            {
                // 故意制造不同类型的异常
                throw new System.ArgumentNullException("testParam", "这是一个测试用的ArgumentNullException");
            }
            catch (System.Exception ex)
            {
                exceptionLogger.Error("捕获到ArgumentNullException", ex);
            }
            
            try
            {
                var array = new int[5];
                var value = array[10]; // 索引越界
            }
            catch (System.Exception ex)
            {
                exceptionLogger.Fatal("捕获到IndexOutOfRangeException", ex);
            }
            
            _logger.Debug("异常处理测试完成");
            yield return null;
        }

        /// <summary>
        /// 测试线程安全（模拟多线程环境）
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator TestThreadSafety()
        {
            _logger.Debug("测试线程安全开始");
            
            var threadLogger = _logManager.GetLogger("ThreadSafetyTest");
            
            // 模拟多个"线程"同时写入日志
            for (int thread = 0; thread < 5; thread++)
            {
                int threadId = thread;
                StartCoroutine(SimulateThreadLogging(threadLogger, threadId));
            }
            
            // 等待所有模拟线程完成
            yield return new WaitForSeconds(1f);
            
            _logger.Debug("线程安全测试完成");
        }

        /// <summary>
        /// 模拟线程日志记录
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="threadId">模拟线程ID</param>
        /// <returns>协程</returns>
        private IEnumerator SimulateThreadLogging(IXLogger logger, int threadId)
        {
            for (int i = 0; i < 10; i++)
            {
                logger.Info($"模拟线程 {threadId} 的日志消息 #{i}");
                yield return new WaitForSeconds(0.05f); // 短暂延迟模拟并发
            }
        }

        /// <summary>
        /// Unity按钮事件：手动运行单次测试
        /// </summary>
        [ContextMenu("运行单次测试")]
        public void RunSingleTest()
        {
            if (_logger == null)
            {
                _logger = _logManager.GetLogger<LoggingTestRunner>();
            }
            
            StartCoroutine(RunSingleTestCoroutine());
        }

        /// <summary>
        /// 运行单次测试的协程
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator RunSingleTestCoroutine()
        {
            _logger.Info("开始运行单次测试");
            
            yield return StartCoroutine(TestBasicLogging());
            yield return StartCoroutine(TestExceptionHandling());
            
            _logger.Info("单次测试完成");
            XLog.FlushAll();
        }

        /// <summary>
        /// Unity按钮事件：测试日志等级过滤
        /// </summary>
        [ContextMenu("测试日志等级过滤")]
        public void TestLogLevelFiltering()
        {
            var filterLogger = _logManager.GetLogger("FilterTest");
            
            _logger.Info("测试日志等级过滤功能");
            
            // 设置不同的最小日志等级并测试
            LogLevel[] levels = { LogLevel.Verbose, LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Fatal };
            
            foreach (var minLevel in levels)
            {
                filterLogger.MinLevel = minLevel;
                _logger.Info($"设置最小日志等级为: {minLevel}");
                
                filterLogger.Verbose("这是Verbose日志");
                filterLogger.Debug("这是Debug日志");
                filterLogger.Info("这是Info日志");
                filterLogger.Warning("这是Warning日志");
                filterLogger.Error("这是Error日志");
                filterLogger.Fatal("这是Fatal日志");
            }
            
            // 恢复默认等级
            filterLogger.MinLevel = LogLevel.Debug;
            _logger.Info("日志等级过滤测试完成");
        }
    }
}
