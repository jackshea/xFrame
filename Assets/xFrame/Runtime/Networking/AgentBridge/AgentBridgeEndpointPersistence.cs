namespace xFrame.Runtime.Networking.AgentBridge
{
    public enum AgentBridgeEndpointLoadResult
    {
        NotFound,
        Loaded,
        Invalid
    }

    public interface IAgentBridgeEndpointPersistence
    {
        bool TrySave(string host, int port, out string error);

        AgentBridgeEndpointLoadResult Load(out string host, out int port, out string error);
    }

    /// <summary>
    ///     Agent Bridge 端点配置持久化。
    /// </summary>
    public sealed class AgentBridgeEndpointPersistence : IAgentBridgeEndpointPersistence
    {
        /// <summary>
        ///     保存当前工程默认首选端点。
        /// </summary>
        public bool TrySave(string host, int port, out string error)
        {
            if (!AgentBridgeOptions.ValidateEndpoint(host, port, out error)) return false;

            var settings = AgentBridgeLocalSettingsStorage.Load(out _) ?? new AgentBridgeLocalSettings();
            settings.Host = host.Trim();
            settings.Port = port;
            AgentBridgeLocalSettingsStorage.Save(settings);
            return true;
        }

        /// <summary>
        ///     加载当前实例首选端点；若当前实例无登记则回退到工程默认端点。
        /// </summary>
        public AgentBridgeEndpointLoadResult Load(out string host, out int port, out string error)
        {
            host = AgentBridgeOptions.DefaultHost;
            port = AgentBridgeOptions.DefaultPort;
            error = null;

            var settings = AgentBridgeLocalSettingsStorage.Load(out error);
            if (settings == null)
                return string.IsNullOrWhiteSpace(error)
                    ? AgentBridgeEndpointLoadResult.NotFound
                    : AgentBridgeEndpointLoadResult.Invalid;

            var currentInstance = AgentBridgeLocalSettingsStorage.LoadCurrentInstance();
            if (TryResolveEndpoint(currentInstance?.Host, currentInstance?.Port, out host, out port, out error))
                return AgentBridgeEndpointLoadResult.Loaded;

            if (TryResolveEndpoint(settings.Host, settings.Port, out host, out port, out error))
                return AgentBridgeEndpointLoadResult.Loaded;

            host = AgentBridgeOptions.DefaultHost;
            port = AgentBridgeOptions.DefaultPort;
            return AgentBridgeEndpointLoadResult.Invalid;
        }

        private static bool TryResolveEndpoint(string storedHost, int? storedPort, out string host, out int port,
            out string error)
        {
            host = AgentBridgeOptions.DefaultHost;
            port = AgentBridgeOptions.DefaultPort;
            error = null;

            if (string.IsNullOrWhiteSpace(storedHost) && !storedPort.HasValue) return false;

            var candidateHost = string.IsNullOrWhiteSpace(storedHost) ? AgentBridgeOptions.DefaultHost : storedHost;
            var candidatePort = storedPort ?? AgentBridgeOptions.DefaultPort;
            if (!AgentBridgeOptions.ValidateEndpoint(candidateHost, candidatePort, out error))
                return false;

            host = candidateHost.Trim();
            port = candidatePort;
            error = null;
            return true;
        }
    }
}
