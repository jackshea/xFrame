using UnityEngine;

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
        public const string HostKey = "xFrame.AgentBridge.Host";

        public const string PortKey = "xFrame.AgentBridge.Port";

        public bool TrySave(string host, int port, out string error)
        {
            if (!AgentBridgeOptions.ValidateEndpoint(host, port, out error)) return false;

            PlayerPrefs.SetString(HostKey, host.Trim());
            PlayerPrefs.SetInt(PortKey, port);
            PlayerPrefs.Save();
            return true;
        }

        public AgentBridgeEndpointLoadResult Load(out string host, out int port, out string error)
        {
            host = AgentBridgeOptions.DefaultHost;
            port = AgentBridgeOptions.DefaultPort;
            error = null;

            var hasHost = PlayerPrefs.HasKey(HostKey);
            var hasPort = PlayerPrefs.HasKey(PortKey);
            if (!hasHost && !hasPort) return AgentBridgeEndpointLoadResult.NotFound;

            var storedHost = PlayerPrefs.GetString(HostKey, AgentBridgeOptions.DefaultHost);
            var storedPort = PlayerPrefs.GetInt(PortKey, AgentBridgeOptions.DefaultPort);
            if (!AgentBridgeOptions.ValidateEndpoint(storedHost, storedPort, out error))
                return AgentBridgeEndpointLoadResult.Invalid;

            host = storedHost.Trim();
            port = storedPort;
            return AgentBridgeEndpointLoadResult.Loaded;
        }
    }
}