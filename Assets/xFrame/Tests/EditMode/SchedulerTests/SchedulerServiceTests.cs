using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Scheduler;

namespace xFrame.Tests
{
    /// <summary>
    /// 调度器服务单元测试
    /// 测试调度器服务的核心功能，包括延迟执行、定时重复执行、下一帧执行、任务取消、暂停和恢复等
    /// </summary>
    [TestFixture]
    public class SchedulerServiceTests
    {
        #region 测试用类

        /// <summary>
        /// 测试用的回调计数器
        /// </summary>
        private class CallbackCounter
        {
            public int Count { get; private set; }
            public readonly List<float> ExecutionTimes;

            public CallbackCounter()
            {
                ExecutionTimes = new List<float>();
            }

            public void Increment()
            {
                Count++;
                ExecutionTimes.Add(UnityEngine.Time.time);
            }

            public void Reset()
            {
                Count = 0;
                ExecutionTimes.Clear();
            }
        }

        #endregion

        private SchedulerService _scheduler;

        [SetUp]
        public void SetUp()
        {
            var logManager = new XLogManager();
            _scheduler = new SchedulerService(logManager);
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler?.Dispose();
            _scheduler = null;
        }

        #region 延迟执行测试

        /// <summary>
        /// 测试基础延迟执行
        /// </summary>
        [Test]
        public void Delay_ShouldExecuteAfterDelay()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float delay = 0.1f;

            // Act
            _scheduler.Delay(delay, counter.Increment);

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(1, counter.Count, "延迟任务应该执行一次");
        }

        /// <summary>
        /// 测试取消延迟任务
        /// </summary>
        [Test]
        public void Delay_Cancel_ShouldNotExecute()
        {
            // Arrange
            var counter = new CallbackCounter();
            var taskId = _scheduler.Delay(0.1f, counter.Increment);

            // Act
            _scheduler.Cancel(taskId);

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(0, counter.Count, "已取消的任务不应该执行");
        }

        /// <summary>
        /// 测试TimeScale不影响
        /// </summary>
        [Test]
        public void Delay_WithoutTimeScale_ShouldIgnoreTimeScale()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float delay = 0.1f;
            var taskId = _scheduler.Delay(delay, counter.Increment, useTimeScale: false);

            // 设置Time.timeScale为0
            UnityEngine.Time.timeScale = 0f;

            // Act - 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // 恢复Time.timeScale
            UnityEngine.Time.timeScale = 1f;

            // Assert
            Assert.AreEqual(1, counter.Count, "不受TimeScale影响的任务应该正常执行");
        }

        /// <summary>
        /// 测试受TimeScale影响
        /// </summary>
        [Test]
        public void Delay_WithTimeScale_ShouldRespectTimeScale()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float delay = 0.05f;
            var taskId = _scheduler.Delay(delay, counter.Increment, useTimeScale: true);

            // 设置Time.timeScale为0
            UnityEngine.Time.timeScale = 0f;

            // Act - 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // 恢复Time.timeScale
            UnityEngine.Time.timeScale = 1f;

            // Assert
            Assert.AreEqual(0, counter.Count, "受TimeScale影响的任务在timeScale=0时不应该执行");
        }

        /// <summary>
        /// 测试多个延迟任务
        /// </summary>
        [Test]
        public void Delay_MultipleTasks_ShouldExecuteAll()
        {
            // Arrange
            var counter = new CallbackCounter();

            // Act
            _scheduler.Delay(0.1f, counter.Increment);
            _scheduler.Delay(0.2f, counter.Increment);
            _scheduler.Delay(0.3f, counter.Increment);

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(3, counter.Count, "所有延迟任务都应该执行");
        }

        #endregion

        #region 定时重复执行测试

        /// <summary>
        /// 测试定时重复执行（固定次数）
        /// </summary>
        [Test]
        public void Interval_ShouldRepeatSpecifiedTimes()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.05f;
            const int repeatCount = 3;

            // Act
            _scheduler.Interval(interval, counter.Increment, repeatCount);

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(repeatCount, counter.Count, $"任务应该执行{repeatCount}次");
        }

        /// <summary>
        /// 测试无限重复执行
        /// </summary>
        [Test]
        public void Interval_InfiniteRepeat_ShouldContinue()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.03f;

            // Act
            var taskId = _scheduler.Interval(interval, counter.Increment, -1);

            // 模拟帧更新
            for (int i = 0; i < 5; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.Greater(counter.Count, 0, "无限重复任务应该持续执行");

            // 清理
            _scheduler.Cancel(taskId);
        }

        /// <summary>
        /// 测试取消间隔任务
        /// </summary>
        [Test]
        public void Interval_Cancel_ShouldStopExecution()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.03f;
            var taskId = _scheduler.Interval(interval, counter.Increment, 10);

            // Act - 运行几帧后取消
            for (int i = 0; i < 2; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }
            var countAfterCancel = counter.Count;
            _scheduler.Cancel(taskId);

            // 继续运行更多帧
            for (int i = 0; i < 5; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(countAfterCancel, counter.Count, "取消后任务应该停止执行");
        }

        /// <summary>
        /// 测试间隔任务的时间间隔
        /// </summary>
        [Test]
        public void Interval_ShouldRespectInterval()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.1f;
            const int repeatCount = 2;

            // Act
            _scheduler.Interval(interval, counter.Increment, repeatCount);

            // 模拟帧更新（假设deltaTime约0.0167秒）
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(repeatCount, counter.Count, "应该执行指定次数");
        }

        #endregion

        #region 下一帧执行测试

        /// <summary>
        /// 测试下一帧执行
        /// </summary>
        [Test]
        public void NextFrame_ShouldExecuteNextFrame()
        {
            // Arrange
            var counter = new CallbackCounter();

            // Act
            _scheduler.NextFrame(counter.Increment);

            // 第一帧
            ((VContainer.Unity.ITickable)_scheduler).Tick();

            // Assert
            Assert.AreEqual(1, counter.Count, "下一帧任务应该在第一次Tick时执行");

            // 第二帧
            ((VContainer.Unity.ITickable)_scheduler).Tick();

            // Assert
            Assert.AreEqual(1, counter.Count, "任务应该只执行一次");
        }

        /// <summary>
        /// 测试取消下一帧任务
        /// </summary>
        [Test]
        public void NextFrame_Cancel_ShouldNotExecute()
        {
            // Arrange
            var counter = new CallbackCounter();
            var taskId = _scheduler.NextFrame(counter.Increment);

            // Act
            _scheduler.Cancel(taskId);
            ((VContainer.Unity.ITickable)_scheduler).Tick();

            // Assert
            Assert.AreEqual(0, counter.Count, "已取消的任务不应该执行");
        }

        #endregion

        #region 暂停和恢复测试

        /// <summary>
        /// 测试暂停和恢复任务
        /// </summary>
        [Test]
        public void PauseAndResume_ShouldWorkCorrectly()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.05f;
            var taskId = _scheduler.Interval(interval, counter.Increment, 10);

            // Act - 运行几帧
            for (int i = 0; i < 3; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }
            var countBeforePause = counter.Count;

            // 暂停任务
            _scheduler.Pause(taskId);

            // 运行更多帧（任务不应该执行）
            for (int i = 0; i < 5; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }
            var countDuringPause = counter.Count;

            // 恢复任务
            _scheduler.Resume(taskId);

            // 运行更多帧（任务应该继续执行）
            for (int i = 0; i < 3; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(countBeforePause, countDuringPause, "暂停时任务不应该执行");
            Assert.Greater(counter.Count, countDuringPause, "恢复后任务应该继续执行");
        }

        /// <summary>
        /// 测试暂停不存在的任务
        /// </summary>
        [Test]
        public void Pause_NonExistentTask_ShouldReturnFalse()
        {
            // Act
            var result = _scheduler.Pause(99999);

            // Assert
            Assert.IsFalse(result, "暂停不存在的任务应该返回false");
        }

        /// <summary>
        /// 测试恢复不存在的任务
        /// </summary>
        [Test]
        public void Resume_NonExistentTask_ShouldReturnFalse()
        {
            // Act
            var result = _scheduler.Resume(99999);

            // Assert
            Assert.IsFalse(result, "恢复不存在的任务应该返回false");
        }

        #endregion

        #region 取消任务测试

        /// <summary>
        /// 测试取消不存在的任务
        /// </summary>
        [Test]
        public void Cancel_NonExistentTask_ShouldReturnFalse()
        {
            // Act
            var result = _scheduler.Cancel(99999);

            // Assert
            Assert.IsFalse(result, "取消不存在的任务应该返回false");
        }

        /// <summary>
        /// 测试取消所有任务
        /// </summary>
        [Test]
        public void CancelAll_ShouldCancelAllTasks()
        {
            // Arrange
            var counter = new CallbackCounter();
            _scheduler.Delay(0.1f, counter.Increment);
            _scheduler.Interval(0.05f, counter.Increment, 5);
            _scheduler.NextFrame(counter.Increment);

            // Act
            _scheduler.CancelAll();

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(0, counter.Count, "取消所有任务后不应该有任何任务执行");
            Assert.AreEqual(0, _scheduler.ActiveTaskCount, "活动任务数量应该为0");
        }

        #endregion

        #region 任务状态查询测试

        /// <summary>
        /// 测试获取任务状态
        /// </summary>
        [Test]
        public void GetTaskStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            var counter = new CallbackCounter();
            var taskId = _scheduler.Delay(0.1f, counter.Increment);

            // Act
            var status = _scheduler.GetTaskStatus(taskId);

            // Assert
            Assert.IsNotNull(status, "应该能够获取任务状态");
            Assert.AreEqual(TaskStatus.Running, status, "任务状态应该是Running");
        }

        /// <summary>
        /// 测试获取不存在任务的状态
        /// </summary>
        [Test]
        public void GetTaskStatus_NonExistentTask_ShouldReturnNull()
        {
            // Act
            var status = _scheduler.GetTaskStatus(99999);

            // Assert
            Assert.IsNull(status, "不存在任务的状态应该返回null");
        }

        /// <summary>
        /// 测试活动任务数量
        /// </summary>
        [Test]
        public void ActiveTaskCount_ShouldBeCorrect()
        {
            // Arrange & Act
            _scheduler.Delay(0.1f, () => { });
            _scheduler.Interval(0.05f, () => { }, 3);
            _scheduler.NextFrame(() => { });

            // Assert
            Assert.AreEqual(3, _scheduler.ActiveTaskCount, "活动任务数量应该正确");

            // 让NextFrameTask执行
            ((VContainer.Unity.ITickable)_scheduler).Tick();

            // Assert
            Assert.AreEqual(2, _scheduler.ActiveTaskCount, "完成任务后数量应该减少");
        }

        #endregion

        #region 异步任务测试

        /// <summary>
        /// 测试异步任务调度
        /// </summary>
        [Test]
        public void ScheduleAsync_ShouldExecuteAsyncMethod()
        {
            // Arrange
            var executed = false;

            // Act - 在 EditMode 测试环境中，直接执行回调而不使用 UniTask.Delay
            var taskId = _scheduler.ScheduleAsync(async (ct) =>
            {
                // 在测试环境中直接执行，不使用 await Delay
                executed = true;
                await UniTask.CompletedTask;
            });

            // 等待任务执行
            System.Threading.Thread.Sleep(10);

            // Assert
            Assert.IsTrue(executed, "异步任务应该被执行");
        }

        /// <summary>
        /// 测试取消异步任务
        /// </summary>
        [Test]
        public void ScheduleAsync_Cancel_ShouldCancelAsyncMethod()
        {
            // Arrange
            var executed = false;
            var cts = new CancellationTokenSource();

            // Act
            var taskId = _scheduler.ScheduleAsync(async (ct) =>
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: ct);
                if (!ct.IsCancellationRequested)
                {
                    executed = true;
                }
            }, cts.Token);

            // 取消任务
            cts.Cancel();
            System.Threading.Thread.Sleep(50);

            // Assert
            Assert.IsFalse(executed, "已取消的异步任务不应该执行完成");
        }

        #endregion

        #region 资源释放测试

        /// <summary>
        /// 测试Dispose应该取消所有任务
        /// </summary>
        [Test]
        public void Dispose_ShouldCancelAllTasks()
        {
            // Arrange
            var counter = new CallbackCounter();
            _scheduler.Delay(0.1f, counter.Increment);
            _scheduler.Interval(0.05f, counter.Increment, 5);

            // Act
            _scheduler.Dispose();

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(0, counter.Count, "Dispose后任务不应该执行");
        }

        /// <summary>
        /// 测试Dispose后Tick不应该抛出异常
        /// </summary>
        [Test]
        public void Tick_AfterDispose_ShouldNotThrow()
        {
            // Arrange
            _scheduler.Delay(0.1f, () => { });

            // Act
            _scheduler.Dispose();

            // Assert
            Assert.DoesNotThrow(() => ((VContainer.Unity.ITickable)_scheduler).Tick(),
                "Dispose后调用Tick不应该抛出异常");
        }

        #endregion

        #region 边界情况测试

        /// <summary>
        /// 测试没有任务时调用Tick
        /// </summary>
        [Test]
        public void Tick_WithNoTasks_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => ((VContainer.Unity.ITickable)_scheduler).Tick(),
                "没有任务时调用Tick不应该抛出异常");
        }

        /// <summary>
        /// 测试延迟时间为负数
        /// </summary>
        [Test]
        public void Delay_NegativeDelay_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _scheduler.Delay(-1f, () => { }),
                "延迟时间为负数应该抛出ArgumentException");
        }

        /// <summary>
        /// 测试间隔时间为0或负数
        /// </summary>
        [Test]
        public void Interval_ZeroOrNegativeInterval_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _scheduler.Interval(0f, () => { }),
                "间隔时间为0应该抛出ArgumentException");

            Assert.Throws<ArgumentException>(() => _scheduler.Interval(-1f, () => { }),
                "间隔时间为负数应该抛出ArgumentException");
        }

        /// <summary>
        /// 测试创建大量任务
        /// </summary>
        [Test]
        public void CreateManyTasks_ShouldHandleCorrectly()
        {
            // Arrange
            var counter = new CallbackCounter();
            const int count = 50;

            // Act
            for (int i = 0; i < count; i++)
            {
                _scheduler.Delay(0.05f, counter.Increment);
            }

            // 模拟帧更新
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(count, counter.Count, "所有任务都应该执行");
        }

        #endregion

        #region 集成测试

        /// <summary>
        /// 测试完整的任务生命周期
        /// </summary>
        [Test]
        public void FullTaskLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var counter = new CallbackCounter();
            const float interval = 0.05f;

            // Act - 创建间隔任务
            var taskId = _scheduler.Interval(interval, counter.Increment, 5);

            // Assert - 任务已创建
            Assert.AreEqual(1, _scheduler.ActiveTaskCount, "任务应该已创建");
            Assert.AreEqual(TaskStatus.Running, _scheduler.GetTaskStatus(taskId), "任务状态应该是Running");

            // Act - 运行几帧
            for (int i = 0; i < 3; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }
            var countBeforePause = counter.Count;

            // Act - 暂停任务
            _scheduler.Pause(taskId);
            Assert.AreEqual(TaskStatus.Paused, _scheduler.GetTaskStatus(taskId), "任务状态应该是Paused");

            // Act - 运行更多帧（不应该执行）
            for (int i = 0; i < 3; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }
            Assert.AreEqual(countBeforePause, counter.Count, "暂停时不应该执行");

            // Act - 恢复任务
            _scheduler.Resume(taskId);
            Assert.AreEqual(TaskStatus.Running, _scheduler.GetTaskStatus(taskId), "任务状态应该是Running");

            // Act - 运行到完成
            for (int i = 0; i < 10; i++)
            {
                ((VContainer.Unity.ITickable)_scheduler).Tick();
            }

            // Assert
            Assert.AreEqual(5, counter.Count, "任务应该执行5次");
            Assert.AreEqual(0, _scheduler.ActiveTaskCount, "任务完成后应该被清理");
            Assert.IsNull(_scheduler.GetTaskStatus(taskId), "完成的任务状态应该为null");
        }

        #endregion
    }
}
