using System;
using System.Security.Cryptography;

namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// SHA256数据校验器实现
    /// 使用SHA256算法计算和验证数据哈希
    /// </summary>
    public class Sha256Validator : IValidator
    {
        /// <summary>
        /// 校验器名称
        /// </summary>
        public string Name => "SHA256";

        /// <summary>
        /// 计算数据的SHA256哈希值
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>32字节的哈希值</returns>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }

        /// <summary>
        /// 验证数据的哈希值是否正确
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="hash">期望的哈希值</param>
        /// <returns>是否验证通过</returns>
        public bool VerifyHash(byte[] data, byte[] hash)
        {
            if (data == null || hash == null)
            {
                return data == null && hash == null;
            }

            var computedHash = ComputeHash(data);
            if (computedHash.Length != hash.Length)
            {
                return false;
            }

            // 使用恒定时间比较防止时序攻击
            var result = 0;
            for (var i = 0; i < computedHash.Length; i++)
            {
                result |= computedHash[i] ^ hash[i];
            }

            return result == 0;
        }
    }
}
