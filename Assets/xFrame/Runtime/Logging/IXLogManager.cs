using System;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志管理器接口
    /// 负责管理整个应用程序的日志系统
    /// </summary>
    public interface IXLogManager
    {
        /// <summary>
        /// 全局日志等级
        /// </summary>
        LogLevel GlobalMinLevel { get; set; }

        /// <summary>
        /// 是否启用全局日志
        /// </summary>
        bool IsGlobalEnabled { get; set; }

        /// <summary>
        /// 获取指定模块的日志记录器
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>日志记录器</returns>
        IXLogger GetLogger(string moduleName);

        /// <summary>
        /// 获取指定类型的日志记录器
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>日志记录器</returns>
        IXLogger GetLogger(Type type);

        /// <summary>
        /// 获取泛型类型的日志记录器
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>日志记录器</returns>
        IXLogger GetLogger<T>();

        /// <summary>
        /// 添加全局日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        void AddGlobalAppender(ILogAppender appender);

        /// <summary>
        /// 移除全局日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        void RemoveGlobalAppender(ILogAppender appender);

        /// <summary>
        /// 刷新所有日志记录器的缓冲区
        /// </summary>
        void FlushAll();

        /// <summary>
        /// 关闭日志系统并释放资源
        /// </summary>
        void Shutdown();
    }
}