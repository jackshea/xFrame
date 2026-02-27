using System;

namespace xFrame.Runtime.Core
{
    /// <summary>
    /// 时间提供者接口 - 抽象时间系统，与Unity解耦
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// 游戏开始到现在经过的时间（秒）
        /// </summary>
        float Time { get; }

        /// <summary>
        /// 距离上一帧经过的时间（秒），受timeScale影响
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        /// 距离上一帧经过的时间（秒），不受timeScale影响
        /// </summary>
        float UnscaledDeltaTime { get; }

        /// <summary>
        /// 时间缩放因子
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 暂停状态
        /// </summary>
        bool IsPaused { get; set; }

        /// <summary>
        /// 帧数
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// 真实时间（不受暂停和时间缩放影响）
        /// </summary>
        float RealTime { get; }

        /// <summary>
        /// 真实时间与上一帧的差值
        /// </summary>
        float RealUnscaledDeltaTime { get; }

        /// <summary>
        /// 更新时间提供者（由Runner调用）
        /// </summary>
        void Tick();

        /// <summary>
        /// 重置时间
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 模拟时间提供者 - 用于测试环境
    /// </summary>
    public class SimulatedTimeProvider : ITimeProvider
    {
        private float _time;
        private float _deltaTime = 1f / 60f;
        private float _timeScale = 1f;
        private bool _isPaused;
        private int _frameCount;
        private float _realTime;
        private float _lastRealTime;

        public float Time => _time;
        public float DeltaTime => _isPaused ? 0 : _deltaTime * _timeScale;
        public float UnscaledDeltaTime => _isPaused ? 0 : _deltaTime;
        public float TimeScale 
        { 
            get => _timeScale; 
            set => _timeScale = Math.Max(0, value); 
        }
        public bool IsPaused 
        { 
            get => _isPaused; 
            set => _isPaused = value; 
        }
        public int FrameCount => _frameCount;
        public float RealTime => _realTime;
        public float RealUnscaledDeltaTime => _isPaused ? 0 : _realTime - _lastRealTime;

        public void Tick()
        {
            _lastRealTime = _realTime;
            _realTime += DeltaTime;
            _time += DeltaTime;
            _frameCount++;
        }

        public void Reset()
        {
            _time = 0;
            _deltaTime = 1f / 60f;
            _timeScale = 1f;
            _isPaused = false;
            _frameCount = 0;
            _realTime = 0;
            _lastRealTime = 0;
        }

        /// <summary>
        /// 推进指定时间
        /// </summary>
        public void Advance(float seconds)
        {
            _time += seconds * _timeScale;
            _realTime += seconds;
        }

        /// <summary>
        /// 设置固定帧间隔
        /// </summary>
        public void SetFixedDeltaTime(float deltaTime)
        {
            _deltaTime = deltaTime;
        }
    }
}
