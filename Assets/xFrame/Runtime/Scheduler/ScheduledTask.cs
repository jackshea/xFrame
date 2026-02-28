using System;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度任务基类
    /// 提供所有调度任务的通用实现
    /// </summary>
    public abstract class ScheduledTask : IScheduledTask
    {
        private static int _nextTaskId = 1;

        /// <summary>
        /// 任务唯一ID
        /// </summary>
        public int TaskId { get; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status { get; protected set; }

        /// <summary>
        /// 是否受 Time.timeScale 影响
        /// </summary>
        public bool UseTimeScale { get; protected set; }

        /// <summary>
        /// 任务优先级（数值越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 任务回调
        /// </summary>
        protected Action Callback { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">任务回调</param>
        /// <param name="useTimeScale">是否受时间缩放影响</param>
        protected ScheduledTask(Action callback, bool useTimeScale)
        {
            TaskId = _nextTaskId++;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            UseTimeScale = useTimeScale;
            Status = TaskStatus.Pending;
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="deltaTime">受时间缩放影响的增量时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的增量时间</param>
        public abstract void Update(float deltaTime, float unscaledDeltaTime);

        /// <summary>
        /// 取消任务
        /// </summary>
        public virtual void Cancel()
        {
            if (Status == TaskStatus.Completed)
                return;

            Status = TaskStatus.Cancelled;
            OnCancel();
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public virtual void Pause()
        {
            if (Status == TaskStatus.Running)
            {
                Status = TaskStatus.Paused;
                OnPause();
            }
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public virtual void Resume()
        {
            if (Status == TaskStatus.Paused)
            {
                Status = TaskStatus.Running;
                OnResume();
            }
        }

        /// <summary>
        /// 执行任务回调
        /// </summary>
        protected virtual void Execute()
        {
            Callback?.Invoke();
            Status = TaskStatus.Completed;
            OnComplete();
        }

        /// <summary>
        /// 任务取消时的回调（子类可重写）
        /// </summary>
        protected virtual void OnCancel() { }

        /// <summary>
        /// 任务暂停时的回调（子类可重写）
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 任务恢复时的回调（子类可重写）
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 任务完成时的回调（子类可重写）
        /// </summary>
        protected virtual void OnComplete() { }
    }
}
