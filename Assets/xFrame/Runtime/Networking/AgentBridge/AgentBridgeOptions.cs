using System;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    ///     Agent Bridge 配置。
    /// </summary>
    public sealed class AgentBridgeOptions
    {
        public const string DefaultHost = "127.0.0.1";

        public const int DefaultPort = 17777;

        /// <summary>
        ///     WebSocket 绑定地址（默认仅本机回环）。
        /// </summary>
        public string Host { get; set; } = DefaultHost;

        /// <summary>
        ///     WebSocket 监听端口。
        /// </summary>
        public int Port { get; set; } = DefaultPort;

        /// <summary>
        ///     主线程分发超时时间（毫秒）。
        /// </summary>
        public int MainThreadTimeoutMs { get; set; } = 120000;

        /// <summary>
        ///     认证 Token。
        /// </summary>
        public string AuthToken { get; set; } = "xframe-dev-token";

        /// <summary>
        ///     是否启用反射桥接命令。
        /// </summary>
        public bool EnableReflectionBridge { get; set; }

        /// <summary>
        ///     允许反射调用的程序集白名单。
        /// </summary>
        public string[] AllowedAssemblies { get; set; } = { "Assembly-CSharp" };

        /// <summary>
        ///     允许反射调用的类型前缀白名单。
        /// </summary>
        public string[] AllowedTypePrefixes { get; set; } = { "xFrame" };

        public static bool ValidateEndpoint(string host, int port, out string error)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                error = "host is required.";
                return false;
            }

            if (port < 1 || port > 65535)
            {
                error = "port must be between 1 and 65535.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TrySetEndpoint(string host, int port, out string error)
        {
            if (!ValidateEndpoint(host, port, out error)) return false;

            Host = host.Trim();
            Port = port;
            return true;
        }

        public bool IsSameEndpoint(string host, int port)
        {
            return string.Equals(Host, host?.Trim(), StringComparison.OrdinalIgnoreCase) && Port == port;
        }
    }
}