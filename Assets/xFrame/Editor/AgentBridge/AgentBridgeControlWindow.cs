using UnityEditor;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Editor.AgentBridge
{
    public sealed class AgentBridgeControlWindow : EditorWindow
    {
        private string _host;
        private string _message;
        private MessageType _messageType;
        private int _port;

        private void OnEnable()
        {
            LoadConfiguredEndpoint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("AgentBridge Control Panel", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var isRunning = AgentBridgeEditorBootstrap.IsRunning;
            var endpoint = AgentBridgeEditorBootstrap.Endpoint;
            if (isRunning)
                EditorGUILayout.HelpBox($"状态: 运行中\nEndpoint: {endpoint}", MessageType.Info);
            else
                EditorGUILayout.HelpBox("状态: 已停止", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Endpoint 设置", EditorStyles.boldLabel);
            _host = EditorGUILayout.TextField("Host / IP", _host ?? string.Empty);
            _port = EditorGUILayout.IntField("Port", _port);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存 Endpoint")) SaveEndpoint();

            if (GUILayout.Button("重置默认值"))
            {
                _host = AgentBridgeOptions.DefaultHost;
                _port = AgentBridgeOptions.DefaultPort;
                _message = "已重置到默认值，点击“保存 Endpoint”生效。";
                _messageType = MessageType.Info;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isRunning);
            if (GUILayout.Button("启动"))
            {
                AgentBridgeEditorBootstrap.EnsureStarted();
                _message = "AgentBridge 已启动。";
                _messageType = MessageType.Info;
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!isRunning);
            if (GUILayout.Button("停止"))
            {
                AgentBridgeEditorBootstrap.Stop();
                _message = "AgentBridge 已停止。";
                _messageType = MessageType.Info;
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(_message))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            if (GUILayout.Button("刷新状态"))
            {
                LoadConfiguredEndpoint();
                Repaint();
            }
        }

        [MenuItem("xFrame/AgentBridge/Control Panel")]
        public static void ShowWindow()
        {
            GetWindow<AgentBridgeControlWindow>("AgentBridge");
        }

        private void SaveEndpoint()
        {
            if (!AgentBridgeEditorBootstrap.SetEndpoint(_host, _port, out var error))
            {
                _message = $"保存失败: {error}";
                _messageType = MessageType.Error;
                return;
            }

            _host = _host?.Trim();
            _message = "Endpoint 已保存。若服务运行中会自动重启生效。";
            _messageType = MessageType.Info;
        }

        private void LoadConfiguredEndpoint()
        {
            var persistence = new AgentBridgeEndpointPersistence();
            var result = persistence.Load(out var host, out var port, out var error);
            _host = host;
            _port = port;

            if (result == AgentBridgeEndpointLoadResult.Invalid)
            {
                _message = $"检测到无效持久化配置，已回退默认值: {error}";
                _messageType = MessageType.Warning;
            }
        }
    }
}