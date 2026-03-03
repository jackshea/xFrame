using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    /// 将后台线程请求分发到 Unity 主线程执行。
    /// </summary>
    internal sealed class EditorMainThreadDispatcher : IDisposable
    {
        private readonly ConcurrentQueue<DispatchWorkItem> _queue = new();
        private readonly int _mainThreadId;
        private bool _disposed;

        public EditorMainThreadDispatcher()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            EditorApplication.update += Pump;
        }

        public string Invoke(Func<string> action, TimeSpan timeout)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EditorMainThreadDispatcher));
            }

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                return action();
            }

            var workItem = new DispatchWorkItem(action);
            _queue.Enqueue(workItem);

            if (!workItem.Task.Wait(timeout))
            {
                workItem.TryAbandon();
                throw new TimeoutException($"AgentBridge 主线程分发超时（{timeout.TotalMilliseconds}ms）。");
            }

            return workItem.Task.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EditorApplication.update -= Pump;

            while (_queue.TryDequeue(out var workItem))
            {
                workItem.Cancel(new ObjectDisposedException(nameof(EditorMainThreadDispatcher)));
            }
        }

        private void Pump()
        {
            while (_queue.TryDequeue(out var workItem))
            {
                workItem.Execute();
            }
        }

        private sealed class DispatchWorkItem
        {
            private readonly Func<string> _action;
            private readonly TaskCompletionSource<string> _completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _state;

            public DispatchWorkItem(Func<string> action)
            {
                _action = action;
            }

            public Task<string> Task => _completionSource.Task;

            public void Execute()
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                {
                    return;
                }

                try
                {
                    _completionSource.TrySetResult(_action());
                }
                catch (Exception ex)
                {
                    _completionSource.TrySetException(ex);
                }
            }

            public void Cancel(Exception exception)
            {
                if (Interlocked.CompareExchange(ref _state, 2, 0) != 0)
                {
                    return;
                }

                _completionSource.TrySetException(exception);
            }

            public void TryAbandon()
            {
                Interlocked.CompareExchange(ref _state, 2, 0);
            }
        }
    }
}
