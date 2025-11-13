using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace xFrame.Runtime.Logging.Appenders
{
    /// <summary>
    /// 文件日志输出器
    /// 将日志输出到文件系统
    /// </summary>
    public class FileLogAppender : ILogAppender
    {
        private readonly bool _autoFlush;
        private readonly string _filePath;
        private readonly ILogFormatter _formatter;
        private readonly object _lock = new();
        private StreamWriter _writer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <param name="formatter">日志格式化器</param>
        /// <param name="autoFlush">是否自动刷新缓冲区</param>
        public FileLogAppender(string filePath, ILogFormatter formatter = null, bool autoFlush = true)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _formatter = formatter ?? new DefaultLogFormatter();
            _autoFlush = autoFlush;

            InitializeWriter();
        }

        /// <summary>
        /// 输出器名称
        /// </summary>
        public string Name => "File";

        /// <summary>
        /// 是否启用该输出器
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小日志等级过滤
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// 写入日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        public void WriteLog(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel || _writer == null)
                return;

            lock (_lock)
            {
                try
                {
                    var formattedMessage = _formatter.Format(entry);
                    _writer.WriteLine(formattedMessage);

                    if (!_autoFlush) _writer.Flush();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"写入文件日志失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                try
                {
                    _writer?.Flush();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"刷新文件日志缓冲区失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Close();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"释放文件日志输出器资源失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化文件写入器
        /// </summary>
        private void InitializeWriter()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _writer = new StreamWriter(_filePath, true, Encoding.UTF8)
                {
                    AutoFlush = _autoFlush
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"无法初始化文件日志输出器: {ex.Message}");
            }
        }
    }
}