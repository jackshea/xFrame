using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Core;

namespace xFrame.Runtime.Core.Scheduler
{
    /// <summary>
    /// 核心调度器服务 - 与Unity完全解耦
    /// </summary>
    public interface ICoreScheduler
    {
        /// <summary>
        /// 延迟执行
        /// </summary>
        int Delay(float delaySeconds, Action callback);

        /// <summary>
        /// 定时重复执行
        /// </summary>
        int Interval(float intervalSeconds, Action callback, int repeatCount = -1);

        /// <summary>
        /// 下一帧执行
        /// </summary>
        int NextFrame(Action callback);

        /// <summary>
        /// 异步方法调度
        /// </summary>
        int ScheduleAsync(Func<System.Threading.CancellationToken, UniTask> asyncAction, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消指定任务
        /// </summary>
        bool Cancel(int taskId);

        /// <summary>
        /// 取消所有任务
        /// </summary>
        void CancelAll();

        /// <summary>
        /// 暂停任务
        /// </summary>
        bool Pause(int taskId);

        /// <summary>
        /// 恢复任务
        /// </summary>
        bool Resume(int taskId);

        /// <summary>
        /// 获取任务状态
        /// </summary>
        TaskStatus? GetTaskStatus(int taskId);

        /// <summary>
        /// 获取当前活动任务数量
        /// </summary>
        int ActiveTaskCount { get; }

        /// <summary>
        /// 更新调度器（由Runner每帧调用）
        /// </summary>
        void Update(float deltaTime, float unscaledDeltaTime);
    }

    /// <summary>
    /// 任务状态
    /// </summary>
    public enum TaskStatus
    {
        Pending,
        Running,
        Paused,
        Completed,
        Cancelled
    }

    /// <summary>
    /// 调度任务接口
    /// </summary>
    public interface IScheduledTask
    {
        int TaskId { get; }
        TaskStatus Status { get; }
        void Update(float deltaTime, float unscaledDeltaTime);
        void Cancel();
        void Pause();
        void Resume();
    }

    /// <summary>
    /// 核心调度器实现
    /// </summary>
    public class CoreScheduler : ICoreScheduler
    {
        private readonly Dictionary<int, IScheduledTask> _tasks = new();
        private readonly List<IScheduledTask> _pendingTasks = new();
        private readonly List<IScheduledTask> _tasksToRemove = new();
        private readonly ICoreLogger _logger;
        private int _nextTaskId = 1;

        public CoreScheduler(ICoreLogManager logManager)
        {
            _logger = logManager?.GetLogger<CoreScheduler>() ?? throw new ArgumentNullException(nameof(logManager));
        }

        public int ActiveTaskCount => _tasks.Count + _pendingTasks.Count;

        public void Update(float deltaTime, float unscaledDeltaTime)
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

            // 更新所有任务
            foreach (var task in _tasks.Values)
            {
                try
                {
                    task.Update(deltaTime, unscaledDeltaTime);
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"任务更新异常: TaskId={task.TaskId}, Status={task.Status}", ex);
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

        public int Delay(float delaySeconds, Action callback)
        {
            var task = new CoreDelayedTask(GetNextTaskId(), delaySeconds, callback, true);
            _pendingTasks.Add(task);
            _logger.Debug($"创建延迟任务: TaskId={task.TaskId}, Delay={delaySeconds}s");
            return task.TaskId;
        }

        public int Interval(float intervalSeconds, Action callback, int repeatCount = -1)
        {
            var task = new CoreIntervalTask(GetNextTaskId(), intervalSeconds, callback, repeatCount, true);
            _pendingTasks.Add(task);
            _logger.Debug($"创建间隔任务: TaskId={task.TaskId}, Interval={intervalSeconds}s, RepeatCount={repeatCount}");
            return task.TaskId;
        }

        public int NextFrame(Action callback)
        {
            var task = new CoreNextFrameTask(GetNextTaskId(), callback);
            _pendingTasks.Add(task);
            _logger.Debug($"创建下一帧任务: TaskId={task.TaskId}");
            return task.TaskId;
        }

        public int ScheduleAsync(Func<System.Threading.CancellationToken, UniTask> asyncAction, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = new CoreCoroutineTask(GetNextTaskId(), asyncAction, cancellationToken);
            _pendingTasks.Add(task);
            _logger.Debug($"创建异步任务: TaskId={task.TaskId}");
            return task.TaskId;
        }

        public bool Cancel(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                _logger.Debug($"取消任务: TaskId={taskId}");
                return true;
            }

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

        public TaskStatus? GetTaskStatus(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                return task.Status;
            }

            foreach (var pendingTask in _pendingTasks)
            {
                if (pendingTask.TaskId == taskId)
                {
                    return pendingTask.Status;
                }
            }

            return null;
        }

        private int GetNextTaskId() => _nextTaskId++;
    }

    // === 核心任务实现 ===

    internal class CoreDelayedTask : IScheduledTask
    {
        public int TaskId { get; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;
        
        private readonly float _delay;
        private readonly Action _callback;
        private readonly bool _useTimeScale;
        private float _elapsed;

        public CoreDelayedTask(int taskId, float delay, Action callback, bool useTimeScale)
        {
            TaskId = taskId;
            _delay = delay;
            _callback = callback;
            _useTimeScale = useTimeScale;
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Status != TaskStatus.Running) return;

            var dt = _useTimeScale ? deltaTime : unscaledDeltaTime;
            _elapsed += dt;

            if (_elapsed >= _delay)
            {
                try
                {
                    _callback?.Invoke();
                }
                finally
                {
                    Status = TaskStatus.Completed;
                }
            }
        }

        public void Cancel() => Status = TaskStatus.Cancelled;
        public void Pause() => Status = TaskStatus.Paused;
        public void Resume()
        {
            if (Status == TaskStatus.Paused)
                Status = TaskStatus.Running;
        }
    }

    internal class CoreIntervalTask : IScheduledTask
    {
        public int TaskId { get; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;

        private readonly float _interval;
        private readonly Action _callback;
        private readonly int _repeatCount;
        private readonly bool _useTimeScale;
        private float _elapsed;
        private int _executedCount;

        public CoreIntervalTask(int taskId, float interval, Action callback, int repeatCount, bool useTimeScale)
        {
            TaskId = taskId;
            _interval = interval;
            _callback = callback;
            _repeatCount = repeatCount;
            _useTimeScale = useTimeScale;
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Status != TaskStatus.Running) return;

            var dt = _useTimeScale ? deltaTime : unscaledDeltaTime;
            _elapsed += dt;

            while (_elapsed >= _interval)
            {
                _elapsed -= _interval;
                _executedCount++;
                _callback?.Invoke();

                if (_repeatCount > 0 && _executedCount >= _repeatCount)
                {
                    Status = TaskStatus.Completed;
                    return;
                }
            }
        }

        public void Cancel() => Status = TaskStatus.Cancelled;
        public void Pause() => Status = TaskStatus.Paused;
        public void Resume()
        {
            if (Status == TaskStatus.Paused)
                Status = TaskStatus.Running;
        }
    }

    internal class CoreNextFrameTask : IScheduledTask
    {
        public int TaskId { get; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;
        private readonly Action _callback;
        private bool _hasRun;

        public CoreNextFrameTask(int taskId, Action callback)
        {
            TaskId = taskId;
            _callback = callback;
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Status != TaskStatus.Running || _hasRun) return;
            
            _hasRun = true;
            _callback?.Invoke();
            Status = TaskStatus.Completed;
        }

        public void Cancel() => Status = TaskStatus.Cancelled;
        public void Pause() => Status = TaskStatus.Paused;
        public void Resume()
        {
            if (Status == TaskStatus.Paused)
                Status = TaskStatus.Running;
        }
    }

    internal class CoreCoroutineTask : IScheduledTask
    {
        public int TaskId { get; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;

        private readonly Func<System.Threading.CancellationToken, UniTask> _asyncAction;
        private readonly System.Threading.CancellationToken _cancellationToken;
        private bool _started;
        private UniTask _task;

        public CoreCoroutineTask(int taskId, Func<System.Threading.CancellationToken, UniTask> asyncAction, System.Threading.CancellationToken cancellationToken)
        {
            TaskId = taskId;
            _asyncAction = asyncAction;
            _cancellationToken = cancellationToken;
        }

        public async void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!_started)
            {
                _started = true;
                Status = TaskStatus.Running;
                _task = _asyncAction(_cancellationToken);
            }

            if (_task.GetAwaiter().IsCompleted)
            {
                try
                {
                    await _task;
                }
                catch (OperationCanceledException)
                {
                    Status = TaskStatus.Cancelled;
                    return;
                }
                catch (Exception)
                {
                    Status = TaskStatus.Completed;
                    return;
                }
                Status = TaskStatus.Completed;
            }
        }

        public void Cancel() => Status = TaskStatus.Cancelled;
        public void Pause() => Status = TaskStatus.Paused;
        public void Resume()
        {
            if (Status == TaskStatus.Paused)
                Status = TaskStatus.Running;
        }
    }
}
