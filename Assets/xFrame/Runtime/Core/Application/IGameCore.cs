using System;
using System.Collections.Generic;

namespace xFrame.Runtime.Core
{
    /// <summary>
    /// 游戏核心层 - 完全与Unity解耦的业务逻辑层
    /// 可以在不运行Unity的情况下执行，用于集成测试和AI模拟
    /// </summary>
    public interface IGameCore
    {
        /// <summary>
        /// 核心层初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 核心层更新 - 每帧调用
        /// </summary>
        void Update(float deltaTime, float unscaledDeltaTime);

        /// <summary>
        /// 核心层销毁
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 核心层是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 获取已注册的服务
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// 获取已注册的服务
        /// </summary>
        object GetService(Type serviceType);
    }

    /// <summary>
    /// 游戏运行器接口 - 驱动核心层运行
    /// </summary>
    public interface IGameRunner
    {
        /// <summary>
        /// 启动游戏
        /// </summary>
        void Run();

        /// <summary>
        /// 停止游戏
        /// </summary>
        void Stop();

        /// <summary>
        /// 游戏是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 获取游戏核心层
        /// </summary>
        IGameCore GameCore { get; }

        /// <summary>
        /// 获取时间提供者
        /// </summary>
        ITimeProvider TimeProvider { get; }

        /// <summary>
        /// 获取日志管理器
        /// </summary>
        ICoreLogManager LogManager { get; }
    }

    /// <summary>
    /// 核心层日志管理器接口
    /// </summary>
    public interface ICoreLogManager
    {
        /// <summary>
        /// 获取日志器
        /// </summary>
        ICoreLogger GetLogger(string category);

        /// <summary>
        /// 获取日志器
        /// </summary>
        ICoreLogger GetLogger<T>();

        /// <summary>
        /// 设置全局日志级别
        /// </summary>
        void SetGlobalLogLevel(LogLevel level);

        /// <summary>
        /// 获取全局日志级别
        /// </summary>
        LogLevel GlobalMinLevel { get; }
    }

    /// <summary>
    /// 核心层日志器接口
    /// </summary>
    public interface ICoreLogger
    {
        string Category { get; }

        void Verbose(string message);
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Fatal(string message, Exception ex = null);
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }
}
