namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// 无加密实现
    /// 直接返回原始数据，用于不需要加密的场景
    /// </summary>
    public class NoEncryptor : IEncryptor
    {
        /// <summary>
        /// 加密器名称
        /// </summary>
        public string Name => "None";

        /// <summary>
        /// 加密数据（直接返回原数据）
        /// </summary>
        /// <param name="plainData">明文数据</param>
        /// <returns>原数据</returns>
        public byte[] Encrypt(byte[] plainData)
        {
            return plainData;
        }

        /// <summary>
        /// 解密数据（直接返回原数据）
        /// </summary>
        /// <param name="cipherData">密文数据</param>
        /// <returns>原数据</returns>
        public byte[] Decrypt(byte[] cipherData)
        {
            return cipherData;
        }
    }
}
