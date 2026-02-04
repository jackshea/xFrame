using System;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 延迟执行任务
    /// 在指定延迟时间后执行一次回调
    /// </summary>
    public class DelayedTask : ScheduledTask
    {
        private float _delaySeconds;
        private float _elapsedTime;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useTimeScale">是否受时间缩放影响</param>
        public DelayedTask(float delaySeconds, Action callback, bool useTimeScale = true)
            : base(callback, useTimeScale)
        {
            if (delaySeconds < 0)
                throw new ArgumentException("延迟时间不能为负数", nameof(delaySeconds));

            _delaySeconds = delaySeconds;
            _elapsedTime = 0f;
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

            // 根据是否使用时间缩放选择对应的deltaTime
            var dt = UseTimeScale ? deltaTime : unscaledDeltaTime;
            _elapsedTime += dt;

            if (_elapsedTime >= _delaySeconds)
            {
                Execute();
            }
        }

        /// <summary>
        /// 任务取消时的回调
        /// </summary>
        protected override void OnCancel()
        {
            _elapsedTime = 0f;
        }
    }
}
