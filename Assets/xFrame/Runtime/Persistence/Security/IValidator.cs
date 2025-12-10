namespace xFrame.Runtime.Persistence.Security
{
    /// <summary>
    /// 数据校验器接口
    /// 定义数据完整性校验操作
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// 校验器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 计算数据的哈希值
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>哈希值</returns>
        byte[] ComputeHash(byte[] data);

        /// <summary>
        /// 验证数据的哈希值是否正确
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="hash">期望的哈希值</param>
        /// <returns>是否验证通过</returns>
        bool VerifyHash(byte[] data, byte[] hash);
    }
}
