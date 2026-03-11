using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace xFrame.Runtime.Networking.AgentBridge
{
    /// <summary>
    ///     Agent Bridge 本地配置文件读写。
    /// </summary>
    public static class AgentBridgeLocalSettingsStorage
    {
        private const string SettingsFileName = "AgentBridgeSettings.json";

        /// <summary>
        ///     本地配置文件完整路径。
        /// </summary>
        public static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, SettingsFileName);

        private static string SettingsDirectoryPath => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory(),
            "UserSettings");

        /// <summary>
        ///     加载本地配置；文件不存在时返回 null。
        /// </summary>
        public static AgentBridgeLocalSettings Load(out string error)
        {
            error = null;
            if (!File.Exists(SettingsFilePath)) return null;

            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<AgentBridgeLocalSettings>(json);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /// <summary>
        ///     保存本地配置。
        /// </summary>
        public static void Save(AgentBridgeLocalSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Directory.CreateDirectory(SettingsDirectoryPath);
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }

    /// <summary>
    ///     Agent Bridge 本地配置模型。
    /// </summary>
    [Serializable]
    public sealed class AgentBridgeLocalSettings
    {
        public string Host;
        public int? Port;
        public string AuthToken;
    }
}
