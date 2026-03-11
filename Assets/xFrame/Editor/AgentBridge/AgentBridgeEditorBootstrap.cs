using System;
using UnityEditor;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking.AgentBridge;

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

        public static bool IsRunning => _server != null && _server.IsRunning;

        public static string Endpoint => _server?.Endpoint;

        public static string InstanceId => _server?.InstanceId ?? AgentBridgeLocalSettingsStorage.CurrentInstanceId;

        public static void EnsureStarted()
        {
            if (_server != null) return;

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

        public static void Stop()
        {
            StopCore(false);
        }

        public static bool SetEndpoint(string host, int port, out string error)
        {
            error = null;

            if (_server == null)
            {
                var persistence = new AgentBridgeEndpointPersistence();
                if (!persistence.TrySave(host, port, out error))
                {
                    Logger.Warning($"AgentBridge endpoint save rejected. host={host}, port={port}, error={error}");
                    return false;
                }

                Logger.Info($"AgentBridge endpoint persisted. endpoint=ws://{host?.Trim()}:{port}");
                return true;
            }

            return _server.SetEndpoint(host, port, out error);
        }

        private static void StopSilently()
        {
            StopCore(true);
        }

        private static void StopCore(bool silent)
        {
            if (_server == null) return;

            try
            {
                _server.Stop();
                if (!silent) Logger.Info("AgentBridge stopped.");
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
