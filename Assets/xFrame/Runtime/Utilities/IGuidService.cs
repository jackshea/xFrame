using System;

namespace xFrame.Runtime.Utilities
{
    /// <summary>
    /// Guid 服务接口。
    /// 封装 Guid 生成与解析，避免业务层直接依赖静态 API。
    /// </summary>
    public interface IGuidService
    {
        /// <summary>
        /// 生成新的 Guid 字符串。
        /// </summary>
        string NewGuid();

        /// <summary>
        /// 尝试解析 Guid 字符串。
        /// </summary>
        bool TryParse(string value, out Guid guid);
    }
}
