namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度任务状态
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 等待中 - 任务已创建但尚未开始
        /// </summary>
        Pending,

        /// <summary>
        /// 运行中 - 任务正在执行
        /// </summary>
        Running,

        /// <summary>
        /// 已暂停 - 任务被暂停，等待恢复
        /// </summary>
        Paused,

        /// <summary>
        /// 已完成 - 任务正常执行完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消 - 任务被取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 调度任务接口
    /// 定义所有调度任务的通用行为
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// 任务唯一ID
        /// </summary>
        int TaskId { get; }

        /// <summary>
        /// 任务状态
        /// </summary>
        TaskStatus Status { get; }

        /// <summary>
        /// 是否受 Time.timeScale 影响
        /// </summary>
        bool UseTimeScale { get; }

        /// <summary>
        /// 任务优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="deltaTime">受时间缩放影响的增量时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的增量时间</param>
        void Update(float deltaTime, float unscaledDeltaTime);

        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();

        /// <summary>
        /// 暂停任务
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复任务
        /// </summary>
        void Resume();
    }
}
