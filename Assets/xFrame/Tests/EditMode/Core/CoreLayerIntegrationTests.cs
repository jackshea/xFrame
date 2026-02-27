using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Core;
using xFrame.Runtime.Core.Events;
using xFrame.Runtime.Core.Scheduler;

namespace xFrame.Tests.Core
{
    /// <summary>
    /// 核心层集成测试 - 验证框架可在无Unity环境下运行
    /// </summary>
    public class CoreLayerIntegrationTests
    {
        private GameRunner _runner;
        private bool _delayCallbackFired;
        private bool _intervalCallbackFired;
        private bool _nextFrameCallbackFired;
        private int _intervalCallbackCount;
        private readonly List<string> _logMessages = new();

        /// <summary>
        /// 测试1: 核心层基本启动流程
        /// </summary>
        public bool TestCoreInitialization()
        {
            Console.WriteLine("=== 测试: 核心层初始化 ===");
            
            try
            {
                _runner = GameRunner.CreateSimulated(60);
                _runner.Run();
                
                bool success = _runner.IsRunning;
                Console.WriteLine($"核心层初始化: {(success ? "成功" : "失败")}");
                
                _runner.Stop();
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"核心层初始化异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试2: 延迟任务
        /// </summary>
        public bool TestDelayTask()
        {
            Console.WriteLine("=== 测试: 延迟任务 ===");
            
            try
            {
                _runner = GameRunner.CreateSimulated(60);
                _runner.Run();
                
                var scheduler = _runner.GetService<ICoreScheduler>();
                
                float delayTime = 0.1f;
                int taskId = scheduler.Delay(delayTime, () => _delayCallbackFired = true);
                
                Console.WriteLine($"创建延迟任务: TaskId={taskId}, Delay={delayTime}s");
                
                // 模拟多帧执行
                SimulateFrames(_runner, 10); // 约0.1秒 (10帧 @ 60fps)
                
                bool success = _delayCallbackFired;
                Console.WriteLine($"延迟任务执行: {(success ? "成功" : "失败")}");
                
                _runner.Stop();
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"延迟任务异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试3: 间隔任务
        /// </summary>
        public bool TestIntervalTask()
        {
            Console.WriteLine("=== 测试: 间隔任务 ===");
            
            try
            {
                _runner = GameRunner.CreateSimulated(60);
                _runner.Run();
                
                var scheduler = _runner.GetService<ICoreScheduler>();
                
                float interval = 0.05f; // 50ms
                int taskId = scheduler.Interval(interval, () => 
                { 
                    _intervalCallbackFired = true;
                    _intervalCallbackCount++;
                }, 3); // 执行3次
                
                Console.WriteLine($"创建间隔任务: TaskId={taskId}, Interval={interval}s, Repeat=3");
                
                // 模拟多帧执行
                SimulateFrames(_runner, 20); // 约0.33秒
                
                bool success = _intervalCallbackCount == 3;
                Console.WriteLine($"间隔任务执行次数: {_intervalCallbackCount} (预期: 3), {(success ? "成功" : "失败")}");
                
                _runner.Stop();
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"间隔任务异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试4: 下一帧任务
        /// </summary>
        public bool TestNextFrameTask()
        {
            Console.WriteLine("=== 测试: 下一帧任务 ===");
            
            try
            {
                _runner = GameRunner.CreateSimulated(60);
                _runner.Run();
                
                var scheduler = _runner.GetService<ICoreScheduler>();
                
                int taskId = scheduler.NextFrame(() => _nextFrameCallbackFired = true);
                
                Console.WriteLine($"创建下一帧任务: TaskId={taskId}");
                
                // 执行一帧
                _runner.Update();
                
                bool success = _nextFrameCallbackFired;
                Console.WriteLine($"下一帧任务执行: {(success ? "成功" : "失败")}");
                
                _runner.Stop();
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下一帧任务异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试5: 任务取消
        /// </summary>
        public bool TestTaskCancellation()
        {
            Console.WriteLine("=== 测试: 任务取消 ===");
            
            try
            {
                _runner = GameRunner.CreateSimulated(60);
                _runner.Run();
                
                var scheduler = _runner.GetService<ICoreScheduler>();
                
                int taskId = scheduler.Delay(1f, () => _delayCallbackFired = true);
                Console.WriteLine($"创建延迟任务: TaskId={taskId}, Delay=1s");
                
                // 取消任务
                bool cancelResult = scheduler.Cancel(taskId);
                Console.WriteLine($"取消任务结果: {cancelResult}");
                
                // 模拟一段时间
                SimulateFrames(_runner, 100);
                
                bool success = cancelResult && !_delayCallbackFired;
                Console.WriteLine($"任务取消: {(success ? "成功" : "失败")}");
                
                _runner.Stop();
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务取消异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试6: 事件总线
        /// </summary>
        public bool TestEventBus()
        {
            Console.WriteLine("=== 测试: 事件总线 ===");
            
            try
            {
                // 初始化事件总线
                CoreEventBus.Initialize();
                
                bool eventReceived = false;
                
                // 订阅事件
                CoreEventBus.Subscribe<TestCoreEvent>((e) => 
                {
                    eventReceived = true;
                    Console.WriteLine($"收到测试事件: Value={e.Value}");
                });
                
                // 发布事件
                var testEvent = new TestCoreEvent { Value = 42 };
                CoreEventBus.Raise(testEvent);
                
                bool success = eventReceived;
                Console.WriteLine($"事件总线: {(success ? "成功" : "失败")}");
                
                // 清理
                CoreEventBus.ClearAll();
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"事件总线异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试7: 日志系统
        /// </summary>
        public bool TestLogging()
        {
            Console.WriteLine("=== 测试: 日志系统 ===");
            
            try
            {
                var logManager = new CoreLogManager();
                var logger = logManager.GetLogger("TestLogger");
                
                // 设置日志级别
                logManager.SetGlobalLogLevel(Core.LogLevel.Debug);
                
                // 记录日志
                logger.Debug("调试信息");
                logger.Info("信息日志");
                logger.Warning("警告日志");
                logger.Error("错误日志");
                
                bool success = true; // 日志系统无异常即成功
                Console.WriteLine($"日志系统: {(success ? "成功" : "失败")}");
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志系统异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试8: 时间提供者
        /// </summary>
        public bool TestTimeProvider()
        {
            Console.WriteLine("=== 测试: 时间提供者 ===");
            
            try
            {
                var timeProvider = new SimulatedTimeProvider();
                
                // 验证初始状态
                if (timeProvider.Time != 0 || timeProvider.FrameCount != 0)
                {
                    Console.WriteLine("初始时间状态异常");
                    return false;
                }
                
                // 推进时间
                timeProvider.Advance(1.0f); // 1秒
                
                if (Math.Abs(timeProvider.Time - 1.0f) > 0.001f)
                {
                    Console.WriteLine("时间推进异常");
                    return false;
                }
                
                // 测试暂停
                timeProvider.IsPaused = true;
                timeProvider.Advance(1.0f);
                
                if (timeProvider.Time > 1.5f) // 暂停时时间不应该增加
                {
                    Console.WriteLine("暂停功能异常");
                    return false;
                }
                
                // 测试时间缩放
                timeProvider.IsPaused = false;
                timeProvider.TimeScale = 2.0f;
                timeProvider.Advance(1.0f);
                
                if (Math.Abs(timeProvider.Time - 2.0f) > 0.001f) // 2x缩放，应该增加2秒
                {
                    Console.WriteLine("时间缩放异常");
                    return false;
                }
                
                Console.WriteLine($"时间提供者: 成功");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"时间提供者异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  核心层集成测试套件");
            Console.WriteLine("========================================\n");
            
            var results = new List<(string Name, bool Success)>
            {
                ("核心层初始化", TestCoreInitialization()),
                ("延迟任务", TestDelayTask()),
                ("间隔任务", TestIntervalTask()),
                ("下一帧任务", TestNextFrameTask()),
                ("任务取消", TestTaskCancellation()),
                ("事件总线", TestEventBus()),
                ("日志系统", TestLogging()),
                ("时间提供者", TestTimeProvider()),
            };
            
            Console.WriteLine("\n========================================");
            Console.WriteLine("  测试结果汇总");
            Console.WriteLine("========================================");
            
            int passed = 0;
            int failed = 0;
            
            foreach (var (name, success) in results)
            {
                Console.WriteLine($"  {(success ? "✓" : "✗")} {name}");
                if (success) passed++;
                else failed++;
            }
            
            Console.WriteLine($"\n  通过: {passed}/{results.Count}");
            Console.WriteLine($"  失败: {failed}/{results.Count}");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 模拟帧执行
        /// </summary>
        private void SimulateFrames(GameRunner runner, int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                runner.Update();
            }
        }

        /// <summary>
        /// 测试用核心事件
        /// </summary>
        private class TestCoreEvent : ICoreEvent
        {
            public int Value { get; set; }
        }

        /// <summary>
        /// 主入口点 - 独立运行测试
        /// </summary>
        public static void Main(string[] args)
        {
            var tests = new CoreLayerIntegrationTests();
            tests.RunAllTests();
        }
    }
}
