namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    /// Agent Bridge 配置。
    /// </summary>
    public sealed class AgentBridgeOptions
    {
        /// <summary>
        /// WebSocket 绑定地址（默认仅本机回环）。
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// WebSocket 监听端口。
        /// </summary>
        public int Port { get; set; } = 17777;

        /// <summary>
        /// 主线程分发超时时间（毫秒）。
        /// </summary>
        public int MainThreadTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 认证 Token。
        /// </summary>
        public string AuthToken { get; set; } = "xframe-dev-token";

        /// <summary>
        /// 是否启用反射桥接命令。
        /// </summary>
        public bool EnableReflectionBridge { get; set; }

        /// <summary>
        /// 允许反射调用的程序集白名单。
        /// </summary>
        public string[] AllowedAssemblies { get; set; } = new[] { "Assembly-CSharp" };

        /// <summary>
        /// 允许反射调用的类型前缀白名单。
        /// </summary>
        public string[] AllowedTypePrefixes { get; set; } = new[] { "xFrame" };
    }
}
