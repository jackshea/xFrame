using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace xFrame.Runtime.Networking
{
    /// <summary>
    /// 空网络客户端默认实现。
    /// 用于框架初始阶段或离线场景，保证依赖可解析。
    /// </summary>
    public sealed class NullNetworkClient : INetworkClient
    {
        private readonly NetworkClientOptions _options;

        public NullNetworkClient(NetworkClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool IsConnected { get; private set; }

        public UniTask ConnectAsync(string endpoint, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint 不能为空", nameof(endpoint));
            }

            ct.ThrowIfCancellationRequested();
            IsConnected = true;
            return UniTask.CompletedTask;
        }

        public UniTask SendAsync(byte[] payload, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsConnected)
            {
                throw new InvalidOperationException("尚未建立网络连接");
            }

            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            _ = _options.SendBufferSize;

            return UniTask.CompletedTask;
        }

        public UniTask<byte[]> ReceiveAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsConnected)
            {
                throw new InvalidOperationException("尚未建立网络连接");
            }

            _ = _options.ReceiveBufferSize;
            return UniTask.FromResult(Array.Empty<byte>());
        }

        public UniTask DisconnectAsync()
        {
            IsConnected = false;
            return UniTask.CompletedTask;
        }
    }
}
