using System;
using System.Collections.Generic;
using Fleck;
using UnityEngine;
using xFrame.Runtime.Logging;
using xFrame.Runtime.Networking.AgentBridge;
using xFrame.Runtime.Networking.AgentBridge.Commands;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    /// 基于 Fleck 的 Unity Agent Bridge 服务。
    /// </summary>
    public sealed class FleckAgentBridgeServer : IDisposable
    {
        private readonly AgentBridgeOptions _options;
        private readonly IXLogger _logger;
        private readonly AgentRpcRouter _router;
        private readonly List<IWebSocketConnection> _connections = new();

        private WebSocketServer _server;

        public FleckAgentBridgeServer(AgentBridgeOptions options = null)
        {
            _options = options ?? new AgentBridgeOptions();
            _logger = new XLogManager().GetLogger("AgentBridge");

            var registry = new AgentCommandRegistry();
            registry.Register(new PingCommandHandler());
            registry.Register(new AuthenticateCommandHandler());
            registry.Register(new ListCommandsHandler());
            registry.Register(new FindGameObjectCommandHandler());
            registry.Register(new InvokeComponentCommandHandler());

            _router = new AgentRpcRouter(_options, registry);
        }

        public bool IsRunning => _server != null;

        public string Endpoint => $"ws://{_options.Host}:{_options.Port}";

        public void Start()
        {
            if (_server != null)
            {
                return;
            }

            var endpoint = Endpoint;
            _server = new WebSocketServer(endpoint);
            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _connections.Add(socket);
                    _logger.Info($"AgentBridge connected: {socket.ConnectionInfo.ClientIpAddress}");
                    Debug.Log($"[AgentBridge] connected: {socket.ConnectionInfo.ClientIpAddress}");
                };

                socket.OnClose = () =>
                {
                    _connections.Remove(socket);
                    _logger.Info("AgentBridge connection closed.");
                    Debug.Log("[AgentBridge] connection closed.");
                };

                socket.OnError = ex =>
                {
                    _logger.Error("AgentBridge socket error.", ex);
                    Debug.LogError($"[AgentBridge] socket error: {ex}");
                };

                socket.OnMessage = message =>
                {
                    var connectionId = socket.ConnectionInfo.Id.ToString();
                    var response = _router.Handle(message, connectionId);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        socket.Send(response);
                    }
                };
            });

            _logger.Info($"AgentBridge started at {endpoint}");
            Debug.Log($"[AgentBridge] server started at {endpoint}");
        }

        public void Stop()
        {
            if (_server == null)
            {
                return;
            }

            try
            {
                foreach (var connection in _connections.ToArray())
                {
                    connection.Close();
                }

                _connections.Clear();
                _server.Dispose();
                _logger.Info("AgentBridge stopped.");
                Debug.Log("[AgentBridge] server stopped.");
            }
            finally
            {
                _server = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
