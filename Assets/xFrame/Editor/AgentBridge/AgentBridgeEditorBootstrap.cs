using System;
using UnityEditor;
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
            AssemblyReloadEvents.beforeAssemblyReload += StopSilently;
            EditorApplication.quitting += StopSilently;
        }

        [MenuItem("xFrame/AgentBridge/Start")]
        public static void EnsureStarted()
        {
            if (_server != null)
            {
                return;
            }

            try
            {
                _server = new FleckAgentBridgeServer();
                _server.Start();
                Logger.Info($"AgentBridge started. Endpoint={_server.Endpoint}");
            }
            catch (Exception ex)
            {
                Logger.Error("AgentBridge start failed.", ex);
                _server?.Dispose();
                _server = null;
            }
        }

        [MenuItem("xFrame/AgentBridge/Start", true)]
        private static bool ValidateStart()
        {
            return _server == null;
        }

        [MenuItem("xFrame/AgentBridge/Stop")]
        public static void Stop()
        {
            StopCore(false);
        }

        [MenuItem("xFrame/AgentBridge/Stop", true)]
        private static bool ValidateStop()
        {
            return _server != null;
        }

        private static void StopSilently()
        {
            StopCore(true);
        }

        private static void StopCore(bool silent)
        {
            if (_server == null)
            {
                return;
            }

            try
            {
                _server.Stop();
                if (!silent)
                {
                    Logger.Info("AgentBridge stopped.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("AgentBridge stop failed.", ex);
            }
            finally
            {
                _server = null;
            }
        }
    }
}
