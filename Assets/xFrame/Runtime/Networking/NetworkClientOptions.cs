namespace xFrame.Runtime.Networking
{
    /// <summary>
    /// 网络客户端选项。
    /// </summary>
    public sealed class NetworkClientOptions
    {
        /// <summary>
        /// 发送缓冲区大小（字节）。
        /// </summary>
        public int SendBufferSize { get; set; } = 8 * 1024;

        /// <summary>
        /// 接收缓冲区大小（字节）。
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 8 * 1024;
    }
}
