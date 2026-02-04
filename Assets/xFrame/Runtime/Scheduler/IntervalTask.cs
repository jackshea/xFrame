using System;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 定时重复执行任务
    /// 按指定间隔重复执行回调，可设置重复次数
    /// </summary>
    public class IntervalTask : ScheduledTask
    {
        private float _intervalSeconds;
        private int _repeatCount; // -1 表示无限重复
        private int _executedCount;
        private float _elapsedTime;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="intervalSeconds">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数（-1表示无限重复）</param>
        /// <param name="useTimeScale">是否受时间缩放影响</param>
        public IntervalTask(float intervalSeconds, Action callback, int repeatCount = -1, bool useTimeScale = true)
            : base(callback, useTimeScale)
        {
            if (intervalSeconds <= 0)
                throw new ArgumentException("间隔时间必须大于0", nameof(intervalSeconds));

            _intervalSeconds = intervalSeconds;
            _repeatCount = repeatCount;
            _executedCount = 0;
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

            // 检查是否已完成所有重复
            if (_repeatCount >= 0 && _executedCount >= _repeatCount)
            {
                Status = TaskStatus.Completed;
                OnComplete();
                return;
            }

            // 根据是否使用时间缩放选择对应的deltaTime
            var dt = UseTimeScale ? deltaTime : unscaledDeltaTime;
            _elapsedTime += dt;

            // 检查是否到达执行时间
            if (_elapsedTime >= _intervalSeconds)
            {
                // 执行回调
                Callback?.Invoke();
                _executedCount++;

                // 重置计时器，保留超过的时间
                _elapsedTime %= _intervalSeconds;

                // 检查是否完成所有重复
                if (_repeatCount >= 0 && _executedCount >= _repeatCount)
                {
                    Status = TaskStatus.Completed;
                    OnComplete();
                }
            }
        }

        /// <summary>
        /// 任务取消时的回调
        /// </summary>
        protected override void OnCancel()
        {
            _elapsedTime = 0f;
            _executedCount = 0;
        }

        /// <summary>
        /// 任务暂停时的回调
        /// </summary>
        protected override void OnPause()
        {
            // 保留当前进度
        }

        /// <summary>
        /// 获取已执行的次数
        /// </summary>
        public int ExecutedCount => _executedCount;

        /// <summary>
        /// 获取剩余的执行次数（-1表示无限）
        /// </summary>
        public int RemainingCount => _repeatCount < 0 ? -1 : _repeatCount - _executedCount;
    }
}
