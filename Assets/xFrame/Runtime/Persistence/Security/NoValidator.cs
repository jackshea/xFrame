using System;

namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// 无校验实现
    /// 不进行任何数据校验，用于不需要校验的场景
    /// </summary>
    public class NoValidator : IValidator
    {
        /// <summary>
        /// 校验器名称
        /// </summary>
        public string Name => "None";

        /// <summary>
        /// 计算哈希（返回空数组）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>空数组</returns>
        public byte[] ComputeHash(byte[] data)
        {
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 验证哈希（始终返回true）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="hash">期望的哈希值</param>
        /// <returns>始终返回true</returns>
        public bool VerifyHash(byte[] data, byte[] hash)
        {
            return true;
        }
    }
}
