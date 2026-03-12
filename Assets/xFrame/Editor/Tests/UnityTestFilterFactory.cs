using System.Text.RegularExpressions;
using UnityEditor.TestTools.TestRunner.Api;

namespace xFrame.Editor.Tests
{
    /// <summary>
    ///     统一构造 Unity Test Runner 使用的过滤条件。
    /// </summary>
    internal static class UnityTestFilterFactory
    {
        /// <summary>
        ///     根据测试模式与用户输入的名称片段创建过滤器。
        ///     用户输入按普通文本处理，转换为可匹配命名空间、类名或方法全名片段的正则。
        /// </summary>
        public static Filter Create(TestMode mode, string filterText)
        {
            var filter = new Filter
            {
                testMode = mode
            };

            if (string.IsNullOrWhiteSpace(filterText))
            {
                return filter;
            }

            filter.groupNames = new[]
            {
                Regex.Escape(filterText.Trim())
            };
            return filter;
        }
    }
}
