using System;
using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Utilities
{
    /// <summary>
    /// 重试工具。
    /// 提供最小异步重试能力，默认固定重试间隔。
    /// </summary>
    public static class RetryUtility
    {
        /// <summary>
        /// 执行异步操作并在失败时重试。
        /// </summary>
        public static async UniTask<T> ExecuteAsync<T>(Func<UniTask<T>> action, int maxRetries, TimeSpan delay)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (maxRetries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "maxRetries 必须大于 0");
            }

            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt == maxRetries)
                    {
                        break;
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        await UniTask.Delay(delay);
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("重试失败，未捕获到具体异常");
        }

        /// <summary>
        /// 执行无返回值异步操作并在失败时重试。
        /// </summary>
        public static async UniTask ExecuteAsync(Func<UniTask> action, int maxRetries, TimeSpan delay)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await ExecuteAsync(async () =>
            {
                await action();
                return true;
            }, maxRetries, delay);
        }
    }
}
