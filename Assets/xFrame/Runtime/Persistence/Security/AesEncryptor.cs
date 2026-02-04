using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// AES加密器实现
    /// 使用AES-256-CBC模式进行数据加密
    /// </summary>
    public class AesEncryptor : IEncryptor
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        /// <summary>
        /// 加密器名称
        /// </summary>
        public string Name => "AES";

        /// <summary>
        /// 使用密钥字符串创建AES加密器
        /// </summary>
        /// <param name="password">密钥字符串</param>
        /// <param name="salt">盐值（可选，默认使用固定盐值）</param>
        public AesEncryptor(string password, string salt = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "密钥不能为空");
            }

            // 使用PBKDF2从密码派生密钥
            var saltBytes = string.IsNullOrEmpty(salt)
                ? Encoding.UTF8.GetBytes("xFrame_Default_Salt_2024")
                : Encoding.UTF8.GetBytes(salt);

            using var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 10000);
            _key = deriveBytes.GetBytes(32); // AES-256需要32字节密钥
            _iv = deriveBytes.GetBytes(16);  // AES需要16字节IV
        }

        /// <summary>
        /// 使用原始密钥和IV创建AES加密器
        /// </summary>
        /// <param name="key">32字节密钥</param>
        /// <param name="iv">16字节IV</param>
        public AesEncryptor(byte[] key, byte[] iv)
        {
            if (key == null || key.Length != 32)
            {
                throw new ArgumentException("密钥必须为32字节", nameof(key));
            }

            if (iv == null || iv.Length != 16)
            {
                throw new ArgumentException("IV必须为16字节", nameof(iv));
            }

            _key = key;
            _iv = iv;
        }

        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="plainData">明文数据</param>
        /// <returns>加密后的数据</returns>
        public byte[] Encrypt(byte[] plainData)
        {
            if (plainData == null || plainData.Length == 0)
            {
                return plainData;
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainData, 0, plainData.Length);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="cipherData">密文数据</param>
        /// <returns>解密后的数据</returns>
        public byte[] Decrypt(byte[] cipherData)
        {
            if (cipherData == null || cipherData.Length == 0)
            {
                return cipherData;
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(cipherData, 0, cipherData.Length);
            }

            return ms.ToArray();
        }
    }
}
