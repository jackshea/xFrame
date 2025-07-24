using System;
using System.Text;

namespace xFrame.Core.Logging
{
    /// <summary>
    /// 默认日志格式化器
    /// 提供标准的日志格式化功能，包含时间戳、线程ID、模块名、等级等信息
    /// </summary>
    public class DefaultLogFormatter : ILogFormatter
    {
        private readonly string _dateTimeFormat;
        private readonly bool _includeThreadId;
        private readonly bool _includeModuleName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dateTimeFormat">时间格式</param>
        /// <param name="includeThreadId">是否包含线程ID</param>
        /// <param name="includeModuleName">是否包含模块名</param>
        public DefaultLogFormatter(string dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff", 
                                 bool includeThreadId = true, 
                                 bool includeModuleName = true)
        {
            _dateTimeFormat = dateTimeFormat;
            _includeThreadId = includeThreadId;
            _includeModuleName = includeModuleName;
        }

        /// <summary>
        /// 格式化日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        /// <returns>格式化后的日志字符串</returns>
        public string Format(LogEntry entry)
        {
            var sb = new StringBuilder();
            
            // 时间戳
            sb.Append('[');
            sb.Append(entry.Timestamp.ToString(_dateTimeFormat));
            sb.Append(']');
            
            // 日志等级
            sb.Append(' ');
            sb.Append('[');
            sb.Append(GetLevelString(entry.Level));
            sb.Append(']');
            
            // 线程ID
            if (_includeThreadId)
            {
                sb.Append(' ');
                sb.Append('[');
                sb.Append("T:");
                sb.Append(entry.ThreadId.ToString("D2"));
                sb.Append(']');
            }
            
            // 模块名
            if (_includeModuleName && !string.IsNullOrEmpty(entry.ModuleName))
            {
                sb.Append(' ');
                sb.Append('[');
                sb.Append(entry.ModuleName);
                sb.Append(']');
            }
            
            // 消息内容
            sb.Append(' ');
            sb.Append(entry.Message);
            
            // 异常信息
            if (entry.Exception != null)
            {
                sb.AppendLine();
                sb.Append("异常信息: ");
                sb.Append(entry.Exception.ToString());
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 获取日志等级的字符串表示
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <returns>等级字符串</returns>
        private string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose: return "VERB";
                case LogLevel.Debug: return "DEBG";
                case LogLevel.Info: return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERRO";
                case LogLevel.Fatal: return "FATL";
                default: return "UNKN";
            }
        }
    }
}
