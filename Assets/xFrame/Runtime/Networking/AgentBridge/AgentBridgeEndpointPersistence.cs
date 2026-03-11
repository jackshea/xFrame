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
        public bool TrySave(string host, int port, out string error)
        {
            if (!AgentBridgeOptions.ValidateEndpoint(host, port, out error)) return false;

            var settings = AgentBridgeLocalSettingsStorage.Load(out _) ?? new AgentBridgeLocalSettings();
            settings.Host = host.Trim();
            settings.Port = port;
            AgentBridgeLocalSettingsStorage.Save(settings);
            return true;
        }

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

            var storedHost = string.IsNullOrWhiteSpace(settings.Host) ? AgentBridgeOptions.DefaultHost : settings.Host;
            var storedPort = settings.Port ?? AgentBridgeOptions.DefaultPort;
            if (!AgentBridgeOptions.ValidateEndpoint(storedHost, storedPort, out error))
                return AgentBridgeEndpointLoadResult.Invalid;

            host = storedHost.Trim();
            port = storedPort;
            return AgentBridgeEndpointLoadResult.Loaded;
        }
    }
}
