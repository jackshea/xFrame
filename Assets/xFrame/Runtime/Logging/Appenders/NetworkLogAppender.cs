using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace xFrame.Runtime.Logging.Appenders
{
    /// <summary>
    /// 网络日志输出器
    /// 将日志通过HTTP发送到远程服务器
    /// </summary>
    public class NetworkLogAppender : ILogAppender
    {
        private readonly string _endpoint;
        private readonly Timer _flushTimer;
        private readonly ILogFormatter _formatter;
        private readonly HttpClient _httpClient;
        private readonly object _lock = new();
        private readonly ConcurrentQueue<string> _logQueue;
        private volatile bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="endpoint">远程日志服务器端点</param>
        /// <param name="formatter">日志格式化器</param>
        /// <param name="flushIntervalMs">刷新间隔（毫秒）</param>
        public NetworkLogAppender(string endpoint, ILogFormatter formatter = null, int flushIntervalMs = 5000)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _formatter = formatter ?? new DefaultLogFormatter();
            _httpClient = new HttpClient();
            _logQueue = new ConcurrentQueue<string>();

            // 定时刷新日志队列
            _flushTimer = new Timer(FlushLogs, null, flushIntervalMs, flushIntervalMs);
        }

        /// <summary>
        /// 输出器名称
        /// </summary>
        public string Name => "Network";

        /// <summary>
        /// 是否启用该输出器
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小日志等级过滤
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// 写入日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        public void WriteLog(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel || _disposed)
                return;

            try
            {
                var formattedMessage = _formatter.Format(entry);
                _logQueue.Enqueue(formattedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"网络日志输出器写入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public void Flush()
        {
            FlushLogs(null);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
            }

            try
            {
                _flushTimer?.Dispose();

                // 最后一次刷新
                FlushLogs(null);

                _httpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"释放网络日志输出器资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 定时刷新日志队列
        /// </summary>
        /// <param name="state">定时器状态</param>
        private async void FlushLogs(object state)
        {
            if (_disposed || _logQueue.IsEmpty)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;
            }

            var logs = new StringBuilder();
            var count = 0;

            // 批量处理日志
            while (_logQueue.TryDequeue(out var log) && count < 100)
            {
                logs.AppendLine(log);
                count++;
            }

            if (count > 0)
                try
                {
                    await SendLogsAsync(logs.ToString());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"发送网络日志失败: {ex.Message}");

                    // 发送失败时，将日志重新入队（避免无限重试）
                    if (count < 10)
                    {
                        var lines = logs.ToString().Split('\n');
                        foreach (var line in lines)
                            if (!string.IsNullOrWhiteSpace(line))
                                _logQueue.Enqueue(line);
                    }
                }
        }

        /// <summary>
        /// 异步发送日志到服务器
        /// </summary>
        /// <param name="logs">日志内容</param>
        /// <returns>发送任务</returns>
        private async Task SendLogsAsync(string logs)
        {
            var content = new StringContent(logs, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();
        }
    }
}