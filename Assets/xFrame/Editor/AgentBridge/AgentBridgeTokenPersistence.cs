using System;
using UnityEngine;
using xFrame.Runtime.Networking.AgentBridge;

namespace xFrame.Editor.AgentBridge
{
    /// <summary>
    ///     Agent Bridge 认证令牌本地持久化。
    /// </summary>
    public sealed class AgentBridgeTokenPersistence
    {
        public string LoadOrCreateToken(out bool createdNewToken)
        {
            var token = LoadToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                createdNewToken = false;
                return token;
            }

            token = Guid.NewGuid().ToString("N");
            SaveToken(token);
            createdNewToken = true;
            return token;
        }

        public string LoadToken()
        {
            var settings = AgentBridgeLocalSettingsStorage.Load(out var error);
            if (settings == null)
            {
                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogWarning($"[AgentBridge] 读取本地 Token 失败，将重新生成。原因: {error}");
                return null;
            }

            return string.IsNullOrWhiteSpace(settings.AuthToken) ? null : settings.AuthToken.Trim();
        }

        private static void SaveToken(string token)
        {
            var settings = AgentBridgeLocalSettingsStorage.Load(out _) ?? new AgentBridgeLocalSettings();
            settings.AuthToken = token;
            AgentBridgeLocalSettingsStorage.Save(settings);
        }
    }
}
