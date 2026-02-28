using System;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 下一帧执行任务
    /// 在下一帧执行一次回调
    /// </summary>
    public class NextFrameTask : ScheduledTask
    {
        private bool _hasExecuted;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        public NextFrameTask(Action callback)
            : base(callback, false)
        {
            _hasExecuted = false;
            Status = TaskStatus.Running;
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="deltaTime">受时间缩放影响的增量时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的增量时间</param>
        public override void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Status == TaskStatus.Completed || Status == TaskStatus.Cancelled)
                return;

            if (Status == TaskStatus.Paused)
                return;

            if (!_hasExecuted)
            {
                Execute();
                _hasExecuted = true;
            }
        }

        /// <summary>
        /// 任务取消时的回调
        /// </summary>
        protected override void OnCancel()
        {
            _hasExecuted = false;
        }
    }
}
