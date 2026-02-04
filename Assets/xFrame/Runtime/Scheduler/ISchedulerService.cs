using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度器服务接口
    /// 提供任务调度功能，包括延迟执行、定时重复执行、下一帧执行和异步方法调度
    /// </summary>
    public interface ISchedulerService
    {
        /// <summary>
        /// 延迟执行
        /// 在指定延迟时间后执行一次回调
        /// </summary>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useTimeScale">是否受Time.timeScale影响</param>
        /// <returns>任务ID</returns>
        int Delay(float delaySeconds, Action callback, bool useTimeScale = true);

        /// <summary>
        /// 定时重复执行
        /// 按指定间隔重复执行回调
        /// </summary>
        /// <param name="intervalSeconds">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数（-1表示无限重复）</param>
        /// <param name="useTimeScale">是否受Time.timeScale影响</param>
        /// <returns>任务ID</returns>
        int Interval(float intervalSeconds, Action callback, int repeatCount = -1, bool useTimeScale = true);

        /// <summary>
        /// 下一帧执行
        /// 在下一帧执行一次回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <returns>任务ID</returns>
        int NextFrame(Action callback);

        /// <summary>
        /// 异步方法调度
        /// 调度并管理异步方法的生命周期
        /// </summary>
        /// <param name="asyncAction">异步操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务ID</returns>
        int ScheduleAsync(Func<CancellationToken, UniTask> asyncAction, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消指定任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功取消</returns>
        bool Cancel(int taskId);

        /// <summary>
        /// 取消所有任务
        /// </summary>
        void CancelAll();

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功暂停</returns>
        bool Pause(int taskId);

        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功恢复</returns>
        bool Resume(int taskId);

        /// <summary>
        /// 获取任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>任务状态（如果任务不存在返回null）</returns>
        TaskStatus? GetTaskStatus(int taskId);

        /// <summary>
        /// 获取当前活动任务数量
        /// </summary>
        int ActiveTaskCount { get; }
    }
}
