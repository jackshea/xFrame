using System;

namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// CRC32数据校验器实现
    /// 使用CRC32算法计算和验证数据校验和
    /// 适用于快速校验场景，安全性低于SHA256
    /// </summary>
    public class Crc32Validator : IValidator
    {
        private static readonly uint[] Crc32Table;

        /// <summary>
        /// 校验器名称
        /// </summary>
        public string Name => "CRC32";

        /// <summary>
        /// 静态构造函数，初始化CRC32查找表
        /// </summary>
        static Crc32Validator()
        {
            Crc32Table = new uint[256];
            const uint polynomial = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                var crc = i;
                for (var j = 0; j < 8; j++)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }

                Crc32Table[i] = crc;
            }
        }

        /// <summary>
        /// 计算数据的CRC32校验和
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>4字节的CRC32值</returns>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var crc = 0xFFFFFFFF;
            foreach (var b in data)
            {
                var index = (crc ^ b) & 0xFF;
                crc = (crc >> 8) ^ Crc32Table[index];
            }

            crc ^= 0xFFFFFFFF;

            // 转换为字节数组（大端序）
            return new[]
            {
                (byte)((crc >> 24) & 0xFF),
                (byte)((crc >> 16) & 0xFF),
                (byte)((crc >> 8) & 0xFF),
                (byte)(crc & 0xFF)
            };
        }

        /// <summary>
        /// 验证数据的CRC32校验和是否正确
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="hash">期望的校验和</param>
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

            for (var i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
