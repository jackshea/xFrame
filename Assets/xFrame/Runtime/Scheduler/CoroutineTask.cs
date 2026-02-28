using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 协程任务（UniTask）
    /// 用于异步方法的调度和生命周期管理
    /// </summary>
    public class CoroutineTask : IScheduledTask
    {
        private static int _nextTaskId = 10000;

        /// <summary>
        /// 任务唯一ID
        /// </summary>
        public int TaskId { get; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status { get; private set; }

        /// <summary>
        /// 是否受 Time.timeScale 影响
        /// </summary>
        public bool UseTimeScale => false;

        /// <summary>
        /// 任务优先级（数值越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;

        private Func<CancellationToken, UniTask> _asyncAction;
        private CancellationTokenSource _cancellationTokenSource;
        private UniTask _task;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="asyncAction">异步操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        public CoroutineTask(Func<CancellationToken, UniTask> asyncAction, CancellationToken cancellationToken = default)
        {
            TaskId = _nextTaskId++;
            _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
            Status = TaskStatus.Pending;

            // 链接外部取消令牌
            if (cancellationToken.CanBeCanceled)
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationToken.Register(() => Cancel());
            }
            else
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            // 启动任务
            _ = StartTaskAsync();
        }

        /// <summary>
        /// 启动异步任务
        /// </summary>
        private async UniTask StartTaskAsync()
        {
            if (Status == TaskStatus.Cancelled)
                return;

            Status = TaskStatus.Running;

            try
            {
                _task = _asyncAction(_cancellationTokenSource.Token);
                await _task;
                Status = TaskStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                Status = TaskStatus.Cancelled;
            }
            catch (Exception)
            {
                Status = TaskStatus.Completed;
            }
        }

        /// <summary>
        /// 更新任务（协程任务不需要每帧更新）
        /// </summary>
        /// <param name="deltaTime">受时间缩放影响的增量时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的增量时间</param>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            // 协程任务不需要每帧更新，由UniTask内部管理
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void Cancel()
        {
            if (Status == TaskStatus.Completed || Status == TaskStatus.Cancelled)
                return;

            Status = TaskStatus.Cancelled;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// 暂停任务（协程任务不支持暂停）
        /// </summary>
        public void Pause()
        {
            // UniTask不支持真正的暂停，这里只是标记状态
            if (Status == TaskStatus.Running)
            {
                Status = TaskStatus.Paused;
            }
        }

        /// <summary>
        /// 恢复任务（协程任务不支持恢复）
        /// </summary>
        public void Resume()
        {
            if (Status == TaskStatus.Paused)
            {
                Status = TaskStatus.Running;
            }
        }
    }
}
