using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using xFrame.Runtime.Scheduler;

namespace xFrame.Tests
{
    /// <summary>
    /// Scheduler 模块 PlayMode 测试
    /// 测试运行时的任务调度功能
    /// </summary>
    [TestFixture]
    public class SchedulerPlayModeTests
    {
        private TestSchedulerHost _testHost;
        private GameObject _testObject;

        /// <summary>
        /// 测试场景设置
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestSchedulerHost");
            _testHost = _testObject.AddComponent<TestSchedulerHost>();
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        [TearDown]
        public void Teardown()
        {
            if (_testHost != null)
            {
                _testHost.CancelAll();
            }

            if (_testObject != null)
            {
                UnityEngine.Object.Destroy(_testObject);
            }
        }

        #region Delay 测试

        /// <summary>
        /// 测试延迟任务执行
        /// </summary>
        [UnityTest]
        public IEnumerator Delay_ShouldExecuteAfterSpecifiedTime()
        {
            // Arrange
            bool executed = false;
            float delayTime = 0.5f;

            _testHost.Delay(delayTime, () => executed = true);

            // Act
            yield return new WaitForSeconds(delayTime - 0.1f);
            Assert.IsFalse(executed, "延迟时间未到时不应执行");

            yield return new WaitForSeconds(0.2f);

            // Assert
            Assert.IsTrue(executed, "延迟时间到达后应执行");
        }

        /// <summary>
        /// 测试多个延迟任务
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleDelayedTasks_ShouldExecuteIndependently()
        {
            // Arrange
            int count1 = 0, count2 = 0, count3 = 0;

            _testHost.Delay(0.3f, () => count1++);
            _testHost.Delay(0.2f, () => count2++);
            _testHost.Delay(0.1f, () => count3++);

            // Act
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(0, count1, "第一个任务不应执行");
            Assert.AreEqual(0, count2, "第二个任务不应执行");
            Assert.AreEqual(1, count3, "第三个任务应已执行");

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(0, count1, "第一个任务不应执行");
            Assert.AreEqual(1, count2, "第二个任务应已执行");

            yield return new WaitForSeconds(0.15f);

            // Assert
            Assert.AreEqual(1, count1, "第一个任务应已执行");
        }

        /// <summary>
        /// 测试不受 Time.timeScale 影响的延迟任务
        /// </summary>
        [UnityTest]
        public IEnumerator Delay_WithUnscaledTime_ShouldIgnoreTimeScale()
        {
            // Arrange
            bool executed = false;
            float delayTime = 0.5f;

            Time.timeScale = 0f; // 暂停游戏时间
            _testHost.Delay(delayTime, () => executed = true, useTimeScale: false);

            // Act
            yield return new WaitForSecondsRealtime(delayTime - 0.1f);
            Assert.IsFalse(executed, "延迟时间未到时不应执行");

            yield return new WaitForSecondsRealtime(0.2f);

            // Assert
            Assert.IsTrue(executed, "不受 Time.timeScale 影响的任务应执行");

            // Cleanup
            Time.timeScale = 1f;
        }

        #endregion

        #region Interval 测试

        /// <summary>
        /// 测试间隔任务执行
        /// </summary>
        [UnityTest]
        public IEnumerator Interval_ShouldExecuteRepeatedly()
        {
            // Arrange
            int executeCount = 0;
            float interval = 0.2f;

            _testHost.Interval(interval, () => executeCount++, repeatCount: 3);

            // Act
            yield return new WaitForSeconds(interval * 2.5f);

            // Assert
            Assert.AreEqual(3, executeCount, "应执行指定次数");
        }

        /// <summary>
        /// 测试无限重复的间隔任务
        /// </summary>
        [UnityTest]
        public IEnumerator Interval_WithInfiniteRepeat_ShouldContinueUntilCancelled()
        {
            // Arrange
            int executeCount = 0;
            float interval = 0.1f;
            int taskId = _testHost.Interval(interval, () => executeCount++);

            // Act - 让任务执行几次
            yield return new WaitForSeconds(interval * 3.5f);

            int beforeCancel = executeCount;
            _testHost.Cancel(taskId);

            yield return new WaitForSeconds(interval * 2f);

            // Assert
            Assert.Greater(beforeCancel, 2, "应至少执行3次");
            Assert.AreEqual(beforeCancel, executeCount, "取消后不应再执行");
        }

        /// <summary>
        /// 测试间隔任务的暂停和恢复
        /// </summary>
        [UnityTest]
        public IEnumerator Interval_PauseAndResume_ShouldWorkCorrectly()
        {
            // Arrange
            int executeCount = 0;
            float interval = 0.1f;
            int taskId = _testHost.Interval(interval, () => executeCount++);

            // Act - 让任务执行几次
            yield return new WaitForSeconds(interval * 2.5f);
            int beforePause = executeCount;

            // 暂停
            _testHost.Pause(taskId);
            yield return new WaitForSeconds(interval * 2f);

            int whilePaused = executeCount;
            Assert.AreEqual(beforePause, whilePaused, "暂停时不应执行");

            // 恢复
            _testHost.Resume(taskId);
            yield return new WaitForSeconds(interval * 1.5f);

            // Assert
            Assert.Greater(executeCount, beforePause, "恢复后应继续执行");
        }

        /// <summary>
        /// 测试不受 Time.timeScale 影响的间隔任务
        /// </summary>
        [UnityTest]
        public IEnumerator Interval_WithUnscaledTime_ShouldIgnoreTimeScale()
        {
            // Arrange
            int executeCount = 0;
            float interval = 0.2f;

            Time.timeScale = 0f; // 暂停游戏时间
            _testHost.Interval(interval, () => executeCount++, repeatCount: 3, useTimeScale: false);

            // Act
            yield return new WaitForSecondsRealtime(interval * 3.5f);

            // Assert
            Assert.AreEqual(3, executeCount, "不受 Time.timeScale 影响的任务应执行");

            // Cleanup
            Time.timeScale = 1f;
        }

        #endregion

        #region NextFrame 测试

        /// <summary>
        /// 测试下一帧执行
        /// </summary>
        [UnityTest]
        public IEnumerator NextFrame_ShouldExecuteOnNextFrame()
        {
            // Arrange
            bool executed = false;

            _testHost.NextFrame(() => executed = true);

            // Act
            Assert.IsFalse(executed, "创建任务时不应执行");
            yield return null; // 等待下一帧

            // Assert
            Assert.IsTrue(executed, "下一帧应执行");
        }

        /// <summary>
        /// 测试多个 NextFrame 任务
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleNextFrame_ShouldAllExecute()
        {
            // Arrange
            int count1 = 0, count2 = 0, count3 = 0;

            _testHost.NextFrame(() => count1++);
            _testHost.NextFrame(() => count2++);
            _testHost.NextFrame(() => count3++);

            // Act
            yield return null; // 等待下一帧

            // Assert
            Assert.AreEqual(1, count1, "第一个任务应执行");
            Assert.AreEqual(1, count2, "第二个任务应执行");
            Assert.AreEqual(1, count3, "第三个任务应执行");
        }

        #endregion

        #region Cancel 测试

        /// <summary>
        /// 测试取消延迟任务
        /// </summary>
        [UnityTest]
        public IEnumerator CancelDelayedTask_ShouldNotExecute()
        {
            // Arrange
            bool executed = false;
            int taskId = _testHost.Delay(0.3f, () => executed = true);

            // Act
            _testHost.Cancel(taskId);
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.IsFalse(executed, "取消的任务不应执行");
        }

        /// <summary>
        /// 测试取消所有任务
        /// </summary>
        [UnityTest]
        public IEnumerator CancelAll_ShouldStopAllTasks()
        {
            // Arrange
            bool executed1 = false;
            bool executed2 = false;
            bool executed3 = false;

            _testHost.Delay(0.2f, () => executed1 = true);
            _testHost.Interval(0.1f, () => executed2 = true, repeatCount: 5);
            _testHost.NextFrame(() => executed3 = true);
            yield return null; // 让 NextFrame 执行

            // Act
            _testHost.CancelAll();
            yield return new WaitForSeconds(0.5f);

            // Assert
            Assert.IsTrue(executed3, "NextFrame 任务应已执行（在 CancelAll 之前）");
            Assert.IsFalse(executed1, "延迟任务不应执行");
            Assert.IsFalse(executed2, "间隔任务不应执行");
        }

        #endregion

        #region TaskStatus 测试

        /// <summary>
        /// 测试获取任务状态
        /// </summary>
        [UnityTest]
        public IEnumerator GetTaskStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            int taskId = _testHost.Delay(0.3f, () => { });

            // Act & Assert
            TaskStatus? status = _testHost.GetTaskStatus(taskId);
            Assert.IsNotNull(status, "应返回任务状态");
            Assert.AreEqual(TaskStatus.Running, status, "初始状态应为 Running");

            yield return new WaitForSeconds(0.4f);

            // 任务完成后状态应不再存在
            status = _testHost.GetTaskStatus(taskId);
            Assert.IsNull(status, "已完成的任务应返回 null");
        }

        /// <summary>
        /// 测试取消任务后的状态
        /// </summary>
        [Test]
        public void GetTaskStatus_AfterCancel_ShouldBeNull()
        {
            // Arrange
            int taskId = _testHost.Delay(0.3f, () => { });

            // Act
            _testHost.Cancel(taskId);
            TaskStatus? status = _testHost.GetTaskStatus(taskId);

            // Assert
            Assert.IsNull(status, "已取消的任务应返回 null");
        }

        #endregion

        #region ActiveTaskCount 测试

        /// <summary>
        /// 测试活动任务计数
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveTaskCount_ShouldTrackCorrectly()
        {
            // Arrange
            Assert.AreEqual(0, _testHost.ActiveTaskCount, "初始活动任务数应为 0");

            // Act
            int task1 = _testHost.Delay(1f, () => { });
            yield return null;
            Assert.AreEqual(1, _testHost.ActiveTaskCount, "添加一个任务后应为 1");

            int task2 = _testHost.Delay(1f, () => { });
            int task3 = _testHost.Delay(1f, () => { });
            yield return null;
            Assert.AreEqual(3, _testHost.ActiveTaskCount, "添加三个任务后应为 3");

            // 取消一个任务
            _testHost.Cancel(task1);
            yield return null;
            Assert.AreEqual(2, _testHost.ActiveTaskCount, "取消一个任务后应为 2");

            // 等待任务完成
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, _testHost.ActiveTaskCount, "所有任务完成后应为 0");
        }

        #endregion

        #region 边界测试

        /// <summary>
        /// 测试大量任务
        /// </summary>
        [UnityTest]
        public IEnumerator LargeNumberOfTasks_ShouldHandleCorrectly()
        {
            // Arrange
            const int taskCount = 100;
            int executedCount = 0;

            for (int i = 0; i < taskCount; i++)
            {
                _testHost.Delay(0.05f + (i * 0.001f), () => executedCount++);
            }

            // Act
            yield return new WaitForSeconds(0.2f);

            // Assert
            Assert.AreEqual(taskCount, executedCount, "所有任务都应执行");
        }

        /// <summary>
        /// 测试在回调中创建新任务
        /// </summary>
        [UnityTest]
        public IEnumerator CreateTaskInCallback_ShouldWork()
        {
            // Arrange
            int outerCount = 0;
            int innerCount = 0;

            _testHost.Delay(0.1f, () =>
            {
                outerCount++;
                // 在回调中创建新任务
                _testHost.Delay(0.05f, () => innerCount++);
            });

            // Act
            yield return new WaitForSeconds(0.05f);
            Assert.AreEqual(0, outerCount, "外部任务不应执行");
            Assert.AreEqual(0, innerCount, "内部任务不应执行");

            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, outerCount, "外部任务应执行");
            Assert.AreEqual(0, innerCount, "内部任务不应执行");

            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.AreEqual(1, innerCount, "内部任务应执行");
        }

        #endregion
    }

    /// <summary>
    /// 测试专用的 Scheduler Host 组件
    /// </summary>
    internal class TestSchedulerHost : MonoBehaviour
    {
        private readonly System.Collections.Generic.Dictionary<int, IScheduledTask> _tasks =
            new System.Collections.Generic.Dictionary<int, IScheduledTask>();
        private readonly System.Collections.Generic.List<IScheduledTask> _pendingTasks =
            new System.Collections.Generic.List<IScheduledTask>();
        private readonly System.Collections.Generic.List<IScheduledTask> _tasksToRemove =
            new System.Collections.Generic.List<IScheduledTask>();
        private int _nextTaskId = 1;

        public int ActiveTaskCount => _tasks.Count + _pendingTasks.Count;

        private void Update()
        {
            // 添加待处理的任务（避免在遍历时修改集合）
            if (_pendingTasks.Count > 0)
            {
                foreach (var task in _pendingTasks)
                {
                    _tasks[task.TaskId] = task;
                }
                _pendingTasks.Clear();
            }

            // 创建任务值的副本用于遍历（避免在遍历时修改集合）
            var tasksSnapshot = new System.Collections.Generic.List<IScheduledTask>(_tasks.Values);
            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;

            foreach (var task in tasksSnapshot)
            {
                task.Update(deltaTime, unscaledDeltaTime);
            }

            // 清理已完成或已取消的任务
            _tasksToRemove.Clear();
            foreach (var task in _tasks.Values)
            {
                if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Cancelled)
                {
                    _tasksToRemove.Add(task);
                }
            }

            foreach (var task in _tasksToRemove)
            {
                _tasks.Remove(task.TaskId);
            }
        }

        public int Delay(float delaySeconds, System.Action callback, bool useTimeScale = true)
        {
            var task = new DelayedTask(delaySeconds, callback, useTimeScale);
            return AddTask(task);
        }

        public int Interval(float intervalSeconds, System.Action callback, int repeatCount = -1, bool useTimeScale = true)
        {
            var task = new IntervalTask(intervalSeconds, callback, repeatCount, useTimeScale);
            return AddTask(task);
        }

        public int NextFrame(System.Action callback)
        {
            var task = new NextFrameTask(callback);
            return AddTask(task);
        }

        public bool Cancel(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                _tasks.Remove(taskId);
                return true;
            }

            // 检查待处理任务
            foreach (var pendingTask in _pendingTasks)
            {
                if (pendingTask.TaskId == taskId)
                {
                    pendingTask.Cancel();
                    _pendingTasks.Remove(pendingTask);
                    return true;
                }
            }

            return false;
        }

        public void CancelAll()
        {
            foreach (var task in _tasks.Values)
            {
                task.Cancel();
            }
            foreach (var task in _pendingTasks)
            {
                task.Cancel();
            }
            _tasks.Clear();
            _pendingTasks.Clear();
        }

        public bool Pause(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Pause();
                return true;
            }
            return false;
        }

        public bool Resume(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Resume();
                return true;
            }
            return false;
        }

        public TaskStatus? GetTaskStatus(int taskId)
        {
            // 先检查运行中的任务
            if (_tasks.TryGetValue(taskId, out var task))
            {
                return task.Status;
            }

            // 再检查待处理的任务
            foreach (var pendingTask in _pendingTasks)
            {
                if (pendingTask.TaskId == taskId)
                {
                    return pendingTask.Status;
                }
            }

            return null;
        }

        private int AddTask(IScheduledTask task)
        {
            int taskId = _nextTaskId++;
            _pendingTasks.Add(task);
            return taskId;
        }
    }
}
