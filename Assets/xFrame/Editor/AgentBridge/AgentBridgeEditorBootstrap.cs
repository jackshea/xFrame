using UnityEditor;
using UnityEngine;
using xFrame.Runtime.Logging;

namespace xFrame.Editor.AgentBridge
{
    [InitializeOnLoad]
    public static class AgentBridgeEditorBootstrap
    {
        private static FleckAgentBridgeServer _server;
        private static readonly IXLogger Logger = new XLogManager().GetLogger("AgentBridge");

        static AgentBridgeEditorBootstrap()
        {
            EditorApplication.delayCall += EnsureStarted;
        }

        [MenuItem("xFrame/AgentBridge/Start")]
        public static void EnsureStarted()
        {
            if (_server != null)
            {
                Logger.Warning("AgentBridge already started.");
                Debug.LogWarning("[AgentBridge] already started.");
                return;
            }

            _server = new FleckAgentBridgeServer();
            _server.Start();
            Logger.Info($"AgentBridge started. Endpoint={_server.Endpoint}");
            Debug.Log($"[AgentBridge] started. Endpoint={_server.Endpoint}");
        }

        [MenuItem("xFrame/AgentBridge/Stop")]
        public static void Stop()
        {
            if (_server == null)
            {
                Logger.Warning("AgentBridge is not running.");
                Debug.LogWarning("[AgentBridge] is not running.");
                return;
            }

            _server.Stop();
            _server = null;
            Logger.Info("AgentBridge stopped.");
            Debug.Log("[AgentBridge] stopped.");
        }
    }
}
