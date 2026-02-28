using System;

namespace xFrame.Runtime.Utilities
{
    /// <summary>
    /// Guid 服务默认实现。
    /// </summary>
    public sealed class GuidService : IGuidService
    {
        public string NewGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        public bool TryParse(string value, out Guid guid)
        {
            return Guid.TryParse(value, out guid);
        }
    }
}
