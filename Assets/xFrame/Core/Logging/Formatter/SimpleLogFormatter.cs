using System;
using System.Text;
using UnityEngine;

namespace xFrame.Core.Logging
{
    /// <summary>
    /// 简单的日志格式化器
    /// 提供标准的日志格式化功能，包含时间戳、等级等信息
    /// </summary>
    public class SimpleLogFormatter : DefaultLogFormatter
    {
        private readonly string _dateTimeFormat;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dateTimeFormat">时间格式</param>
        /// <param name="includeThreadId">是否包含线程ID</param>
        /// <param name="includeModuleName">是否包含模块名</param>
        public SimpleLogFormatter(string dateTimeFormat = "HH:mm:ss.fff")
        {
            _dateTimeFormat = dateTimeFormat;
        }

        /// <summary>
        /// 格式化日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        /// <returns>格式化后的日志字符串</returns>
        public override string Format(LogEntry entry)
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
    }
}
