namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// 加密器接口
    /// 定义数据加密和解密操作
    /// </summary>
    public interface IEncryptor
    {
        /// <summary>
        /// 加密器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="plainData">明文数据</param>
        /// <returns>加密后的数据</returns>
        byte[] Encrypt(byte[] plainData);

        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="cipherData">密文数据</param>
        /// <returns>解密后的数据</returns>
        byte[] Decrypt(byte[] cipherData);
    }
}
