using System;

namespace xFrame.Runtime.Core
{
    /// <summary>
    ///     时间提供者接口 - 抽象时间系统，与Unity解耦
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        ///     游戏开始到现在经过的时间（秒）
        /// </summary>
        float Time { get; }

        /// <summary>
        ///     距离上一帧经过的时间（秒），受timeScale影响
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        ///     距离上一帧经过的时间（秒），不受timeScale影响
        /// </summary>
        float UnscaledDeltaTime { get; }

        /// <summary>
        ///     时间缩放因子
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        ///     暂停状态
        /// </summary>
        bool IsPaused { get; set; }

        /// <summary>
        ///     帧数
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        ///     真实时间（不受暂停和时间缩放影响）
        /// </summary>
        float RealTime { get; }

        /// <summary>
        ///     真实时间与上一帧的差值
        /// </summary>
        float RealUnscaledDeltaTime { get; }

        /// <summary>
        ///     更新时间提供者（由Runner调用）
        /// </summary>
        void Tick();

        /// <summary>
        ///     重置时间
        /// </summary>
        void Reset();
    }

    /// <summary>
    ///     模拟时间提供者 - 用于测试环境
    /// </summary>
    public class SimulatedTimeProvider : ITimeProvider
    {
        private float _deltaTime = 1f / 60f;
        private float _lastRealTime;
        private float _timeScale = 1f;

        public float Time { get; private set; }

        public float DeltaTime => IsPaused ? 0 : _deltaTime * _timeScale;
        public float UnscaledDeltaTime => IsPaused ? 0 : _deltaTime;

        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = Math.Max(0, value);
        }

        public bool IsPaused { get; set; }

        public int FrameCount { get; private set; }

        public float RealTime { get; private set; }

        public float RealUnscaledDeltaTime => IsPaused ? 0 : RealTime - _lastRealTime;

        public void Tick()
        {
            _lastRealTime = RealTime;
            RealTime += DeltaTime;
            Time += DeltaTime;
            FrameCount++;
        }

        public void Reset()
        {
            Time = 0;
            _deltaTime = 1f / 60f;
            _timeScale = 1f;
            IsPaused = false;
            FrameCount = 0;
            RealTime = 0;
            _lastRealTime = 0;
        }

        /// <summary>
        ///     推进指定时间
        /// </summary>
        public void Advance(float seconds)
        {
            Time += seconds * _timeScale;
            RealTime += seconds;
        }

        /// <summary>
        ///     设置固定帧间隔
        /// </summary>
        public void SetFixedDeltaTime(float deltaTime)
        {
            _deltaTime = deltaTime;
        }
    }
}