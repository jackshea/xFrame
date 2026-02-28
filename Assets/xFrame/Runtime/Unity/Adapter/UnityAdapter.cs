using UnityEngine;
using xFrame.Runtime.Core;

namespace xFrame.Runtime.Unity.Adapter
{
    /// <summary>
    /// Unity时间提供者适配器
    /// 将Unity的Time类适配到ITimeProvider接口
    /// </summary>
    public class UnityTimeProvider : ITimeProvider
    {
        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
        public float TimeScale 
        { 
            get => UnityEngine.Time.timeScale; 
            set => UnityEngine.Time.timeScale = value; 
        }
        public bool IsPaused { get; set; }
        public int FrameCount => UnityEngine.Time.frameCount;
        public float RealTime => UnityEngine.Time.realtimeSinceStartup;
        public float RealUnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;

        public void Tick()
        {
            // Unity时间由Unity引擎本身更新，这里不需要额外操作
            // 此方法为接口一致性保留
        }

        public void Reset()
        {
            // Unity时间无法重置，抛出异常或记录警告
            Debug.LogWarning("UnityTimeProvider 不支持重置时间，因为时间由Unity引擎管理");
        }
    }

    /// <summary>
    /// Unity日志适配器 - 将核心日志输出到Unity控制台
    /// </summary>
    public class UnityLogAppender : ICoreLogAppender
    {
        public void Append(CoreLogEntry entry)
        {
            var message = $"[{entry.Category}] {entry.Message}";
            
            switch (entry.Level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(message);
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    if (entry.Exception != null)
                        UnityEngine.Debug.LogError($"{message}\n{entry.Exception}");
                    else
                        UnityEngine.Debug.LogError(message);
                    break;
            }
        }
    }

    /// <summary>
    /// Unity生命周期适配器 - 将Unity生命周期事件转换为核心层接口
    /// </summary>
    public class UnityLifecycleAdapter : MonoBehaviour
    {
        public System.Action OnUpdate;
        public System.Action OnLateUpdate;
        public System.Action OnFixedUpdate;
        public System.Action OnDestroyAction;
        public System.Action OnApplicationQuitAction;

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        private void OnDestroy()
        {
            OnDestroyAction?.Invoke();
        }

        private void OnApplicationQuit()
        {
            OnApplicationQuitAction?.Invoke();
        }
    }
}
