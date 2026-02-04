using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using xFrame.Runtime.Logging;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度器服务实现
    /// 提供任务调度功能，实现VContainer的ITickable接口自动更新
    /// </summary>
    public class SchedulerService : ISchedulerService, ITickable, IDisposable
    {
        private readonly Dictionary<int, IScheduledTask> _tasks;
        private readonly List<IScheduledTask> _pendingTasks;
        private readonly List<IScheduledTask> _tasksToRemove;
        private readonly IXLogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logManager">日志管理器</param>
        public SchedulerService(IXLogManager logManager)
        {
            _tasks = new Dictionary<int, IScheduledTask>();
            _pendingTasks = new List<IScheduledTask>();
            _tasksToRemove = new List<IScheduledTask>();
            _logger = logManager.GetLogger<SchedulerService>();
        }

        /// <summary>
        /// 获取当前活动任务数量（包括待处理和运行中的任务）
        /// </summary>
        public int ActiveTaskCount => _tasks.Count + _pendingTasks.Count;

        /// <summary>
        /// VContainer的Tick回调，每帧自动调用
        /// </summary>
        void ITickable.Tick()
        {
            Update();
        }

        /// <summary>
        /// 更新所有任务
        /// </summary>
        private void Update()
        {
            // 添加待处理的任务
            if (_pendingTasks.Count > 0)
            {
                foreach (var task in _pendingTasks)
                {
                    _tasks[task.TaskId] = task;
                }
                _pendingTasks.Clear();
            }

            // 获取时间增量，应用timeScale
            var unscaledDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            var deltaTime = unscaledDeltaTime * UnityEngine.Time.timeScale;

            foreach (var task in _tasks.Values)
            {
                try
                {
                    task.Update(deltaTime, unscaledDeltaTime);
                }
                catch (Exception ex)
                {
                    _logger.Error($"任务更新异常: TaskId={task.TaskId}, Status={task.Status}", ex);
                    _tasksToRemove.Add(task);
                }
            }

            // 清理已完成或已取消的任务
            foreach (var task in _tasks.Values)
            {
                if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Cancelled)
                {
                    _tasksToRemove.Add(task);
                }
            }

            if (_tasksToRemove.Count > 0)
            {
                foreach (var task in _tasksToRemove)
                {
                    _tasks.Remove(task.TaskId);
                }
                _tasksToRemove.Clear();
            }
        }

        /// <summary>
        /// 延迟执行
        /// </summary>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useTimeScale">是否受Time.timeScale影响</param>
        /// <returns>任务ID</returns>
        public int Delay(float delaySeconds, Action callback, bool useTimeScale = true)
        {
            var task = new DelayedTask(delaySeconds, callback, useTimeScale);
            _pendingTasks.Add(task);
            _logger.Debug($"创建延迟任务: TaskId={task.TaskId}, Delay={delaySeconds}s, UseTimeScale={useTimeScale}");
            return task.TaskId;
        }

        /// <summary>
        /// 定时重复执行
        /// </summary>
        /// <param name="intervalSeconds">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数（-1表示无限重复）</param>
        /// <param name="useTimeScale">是否受Time.timeScale影响</param>
        /// <returns>任务ID</returns>
        public int Interval(float intervalSeconds, Action callback, int repeatCount = -1, bool useTimeScale = true)
        {
            var task = new IntervalTask(intervalSeconds, callback, repeatCount, useTimeScale);
            _pendingTasks.Add(task);
            _logger.Debug($"创建间隔任务: TaskId={task.TaskId}, Interval={intervalSeconds}s, RepeatCount={repeatCount}, UseTimeScale={useTimeScale}");
            return task.TaskId;
        }

        /// <summary>
        /// 下一帧执行
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <returns>任务ID</returns>
        public int NextFrame(Action callback)
        {
            var task = new NextFrameTask(callback);
            _pendingTasks.Add(task);
            _logger.Debug($"创建下一帧任务: TaskId={task.TaskId}");
            return task.TaskId;
        }

        /// <summary>
        /// 异步方法调度
        /// </summary>
        /// <param name="asyncAction">异步操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务ID</returns>
        public int ScheduleAsync(Func<CancellationToken, UniTask> asyncAction, CancellationToken cancellationToken = default)
        {
            var task = new CoroutineTask(asyncAction, cancellationToken);
            _pendingTasks.Add(task);
            _logger.Debug($"创建异步任务: TaskId={task.TaskId}");
            return task.TaskId;
        }

        /// <summary>
        /// 取消指定任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功取消</returns>
        public bool Cancel(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                _logger.Debug($"取消任务: TaskId={taskId}");
                return true;
            }

            // 检查待处理任务
            foreach (var pendingTask in _pendingTasks)
            {
                if (pendingTask.TaskId == taskId)
                {
                    pendingTask.Cancel();
                    _pendingTasks.Remove(pendingTask);
                    _logger.Debug($"取消待处理任务: TaskId={taskId}");
                    return true;
                }
            }

            _logger.Warning($"取消任务失败，任务不存在: TaskId={taskId}");
            return false;
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll()
        {
            var count = _tasks.Count + _pendingTasks.Count;
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
            _logger.Debug($"取消所有任务: Count={count}");
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功暂停</returns>
        public bool Pause(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Pause();
                _logger.Debug($"暂停任务: TaskId={taskId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功恢复</returns>
        public bool Resume(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Resume();
                _logger.Debug($"恢复任务: TaskId={taskId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>任务状态（如果任务不存在返回null）</returns>
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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            CancelAll();
            _logger.Info("SchedulerService已销毁");
        }
    }
}
