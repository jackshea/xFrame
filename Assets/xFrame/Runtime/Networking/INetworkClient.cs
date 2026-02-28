using System.Threading;
using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Networking
{
    /// <summary>
    /// 网络客户端接口。
    /// 提供最小可用的连接与收发能力，便于后续替换为具体协议实现。
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        /// 当前是否已连接。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接到指定端点。
        /// </summary>
        /// <param name="endpoint">连接端点</param>
        /// <param name="ct">取消令牌</param>
        UniTask ConnectAsync(string endpoint, CancellationToken ct = default);

        /// <summary>
        /// 发送二进制数据。
        /// </summary>
        /// <param name="payload">数据负载</param>
        /// <param name="ct">取消令牌</param>
        UniTask SendAsync(byte[] payload, CancellationToken ct = default);

        /// <summary>
        /// 接收二进制数据。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>接收到的数据</returns>
        UniTask<byte[]> ReceiveAsync(CancellationToken ct = default);

        /// <summary>
        /// 断开连接。
        /// </summary>
        UniTask DisconnectAsync();
    }
}
