using System;
using System.Collections.Generic;
using System.IO;

namespace xFrame.Runtime.Core
{
    /// <summary>
    /// 核心日志管理器实现 - 与Unity解耦
    /// </summary>
    public class CoreLogManager : ICoreLogManager
    {
        private readonly Dictionary<string, ICoreLogger> _loggers = new();
        private LogLevel _globalMinLevel = LogLevel.Debug;
        private readonly List<ICoreLogAppender> _appenders = new();
        private readonly object _lock = new();

        public CoreLogManager()
        {
            // 默认添加控制台输出
            AddAppender(new ConsoleLogAppender());
        }

        public CoreLogManager(IEnumerable<ICoreLogAppender> appenders)
        {
            if (appenders != null)
            {
                foreach (var appender in appenders)
                {
                    _appenders.Add(appender);
                }
            }
        }

        public ICoreLogger GetLogger(string category)
        {
            lock (_lock)
            {
                if (!_loggers.TryGetValue(category, out var logger))
                {
                    logger = new CoreLogger(category, this);
                    _loggers[category] = logger;
                }
                return logger;
            }
        }

        public ICoreLogger GetLogger<T>()
        {
            return GetLogger(typeof(T).FullName ?? typeof(T).Name);
        }

        public void SetGlobalLogLevel(LogLevel level)
        {
            _globalMinLevel = level;
        }

        public LogLevel GlobalMinLevel => _globalMinLevel;

        public void AddAppender(ICoreLogAppender appender)
        {
            if (appender != null)
            {
                _appenders.Add(appender);
            }
        }

        public void RemoveAppender(ICoreLogAppender appender)
        {
            _appenders.Remove(appender);
        }

        internal void Log(string category, LogLevel level, string message, Exception exception = null)
        {
            if (level < _globalMinLevel)
                return;

            lock (_lock)
            {
                var entry = new CoreLogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Category = category,
                    Message = message,
                    Exception = exception
                };

                foreach (var appender in _appenders)
                {
                    try
                    {
                        appender.Append(entry);
                    }
                    catch
                    {
                        // 忽略appender异常，避免级联失败
                    }
                }
            }
        }
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public struct CoreLogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public string Category;
        public string Message;
        public Exception Exception;
    }

    /// <summary>
    /// 日志输出器接口
    /// </summary>
    public interface ICoreLogAppender
    {
        void Append(CoreLogEntry entry);
    }

    /// <summary>
    /// 控制台日志输出器
    /// </summary>
    public class ConsoleLogAppender : ICoreLogAppender
    {
        public void Append(CoreLogEntry entry)
        {
            var prefix = $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}]";
            
            if (entry.Exception != null)
            {
                Console.WriteLine($"{prefix} {entry.Message}\n{entry.Exception}");
            }
            else
            {
                Console.WriteLine($"{prefix} {entry.Message}");
            }
        }
    }

    /// <summary>
    /// 文件日志输出器
    /// </summary>
    public class FileLogAppender : ICoreLogAppender
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public FileLogAppender(string filePath)
        {
            _filePath = filePath;
        }

        public void Append(CoreLogEntry entry)
        {
            lock (_lock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var prefix = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}]";
                    var line = entry.Exception != null 
                        ? $"{prefix} {entry.Message}\n{entry.Exception}\n"
                        : $"{prefix} {entry.Message}\n";

                    File.AppendAllText(_filePath, line);
                }
                catch
                {
                    // 忽略文件写入异常
                }
            }
        }
    }

    /// <summary>
    /// 核心日志器实现
    /// </summary>
    internal class CoreLogger : ICoreLogger
    {
        private readonly CoreLogManager _manager;

        public string Category { get; }

        public CoreLogger(string category, CoreLogManager manager)
        {
            Category = category;
            _manager = manager;
        }

        public void Verbose(string message) => _manager.Log(Category, LogLevel.Verbose, message);
        public void Debug(string message) => _manager.Log(Category, LogLevel.Debug, message);
        public void Info(string message) => _manager.Log(Category, LogLevel.Info, message);
        public void Warning(string message) => _manager.Log(Category, LogLevel.Warning, message);
        public void Error(string message) => _manager.Log(Category, LogLevel.Error, message);
        public void Fatal(string message, Exception ex = null) => _manager.Log(Category, LogLevel.Fatal, message, ex);
    }
}
