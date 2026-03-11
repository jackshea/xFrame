using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private static readonly object SyncRoot = new();

        /// <summary>
        ///     本地配置文件完整路径。
        /// </summary>
        public static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, SettingsFileName);

        /// <summary>
        ///     当前 Unity 进程对应的实例标识。
        /// </summary>
        public static string CurrentInstanceId =>
            $"{GetProjectPath()}::{Process.GetCurrentProcess().Id}";

        /// <summary>
        ///     当前 Unity 进程 Id。
        /// </summary>
        public static int CurrentProcessId => Process.GetCurrentProcess().Id;

        private static string SettingsDirectoryPath => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory(),
            "UserSettings");

        /// <summary>
        ///     加载本地配置；文件不存在时返回 null。
        /// </summary>
        public static AgentBridgeLocalSettings Load(out string error)
        {
            lock (SyncRoot)
            {
                return LoadInternal(out error);
            }
        }

        /// <summary>
        ///     保存本地配置。
        /// </summary>
        public static void Save(AgentBridgeLocalSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            lock (SyncRoot)
            {
                SaveInternal(settings);
            }
        }

        /// <summary>
        ///     获取当前 Unity 实例的运行登记。
        /// </summary>
        public static AgentBridgeInstanceRegistration LoadCurrentInstance()
        {
            var settings = Load(out _) ?? new AgentBridgeLocalSettings();
            return settings.Instances?.FirstOrDefault(instance =>
                string.Equals(instance?.InstanceId, CurrentInstanceId, StringComparison.Ordinal));
        }

        /// <summary>
        ///     更新当前 Unity 实例的运行登记。
        /// </summary>
        public static void UpsertCurrentInstance(string host, int port, bool isRunning)
        {
            lock (SyncRoot)
            {
                var settings = LoadInternal(out _) ?? new AgentBridgeLocalSettings();
                settings.Instances ??= new List<AgentBridgeInstanceRegistration>();

                var instance = settings.Instances.FirstOrDefault(item =>
                    string.Equals(item?.InstanceId, CurrentInstanceId, StringComparison.Ordinal));
                if (instance == null)
                {
                    instance = new AgentBridgeInstanceRegistration();
                    settings.Instances.Add(instance);
                }

                instance.InstanceId = CurrentInstanceId;
                instance.ProcessId = CurrentProcessId;
                instance.ProjectPath = GetProjectPath();
                instance.Host = host?.Trim();
                instance.Port = port;
                instance.IsRunning = isRunning;
                instance.LastSeenUtc = DateTime.UtcNow.ToString("O");

                SaveInternal(settings);
            }
        }

        /// <summary>
        ///     将当前 Unity 实例标记为停止。
        /// </summary>
        public static void MarkCurrentInstanceStopped()
        {
            lock (SyncRoot)
            {
                var settings = LoadInternal(out _) ?? new AgentBridgeLocalSettings();
                if (settings.Instances == null || settings.Instances.Count == 0) return;

                var instance = settings.Instances.FirstOrDefault(item =>
                    string.Equals(item?.InstanceId, CurrentInstanceId, StringComparison.Ordinal));
                if (instance == null) return;

                instance.IsRunning = false;
                instance.LastSeenUtc = DateTime.UtcNow.ToString("O");
                SaveInternal(settings);
            }
        }

        /// <summary>
        ///     获取当前工程登记过的所有实例。
        /// </summary>
        public static IReadOnlyList<AgentBridgeInstanceRegistration> LoadProjectInstances(out string error)
        {
            var settings = Load(out error);
            var projectPath = GetProjectPath();
            var instances = settings?.Instances ?? new List<AgentBridgeInstanceRegistration>();
            return instances
                .Where(instance => instance != null &&
                                   string.Equals(instance.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static AgentBridgeLocalSettings LoadInternal(out string error)
        {
            error = null;
            if (!File.Exists(SettingsFilePath)) return null;

            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<AgentBridgeLocalSettings>(json);
                settings ??= new AgentBridgeLocalSettings();
                settings.Instances ??= new List<AgentBridgeInstanceRegistration>();
                return settings;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static void SaveInternal(AgentBridgeLocalSettings settings)
        {
            settings.Instances ??= new List<AgentBridgeInstanceRegistration>();
            Directory.CreateDirectory(SettingsDirectoryPath);
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static string GetProjectPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        }
    }

    /// <summary>
    ///     Agent Bridge 本地配置模型。
    /// </summary>
    [Serializable]
    public sealed class AgentBridgeLocalSettings
    {
        /// <summary>
        ///     默认首选 Host。
        /// </summary>
        public string Host;

        /// <summary>
        ///     默认首选 Port。
        /// </summary>
        public int? Port;

        /// <summary>
        ///     兼容旧版本配置文件保留字段。
        /// </summary>
        public string AuthToken;

        /// <summary>
        ///     当前工程下的实例运行登记。
        /// </summary>
        public List<AgentBridgeInstanceRegistration> Instances = new();
    }

    /// <summary>
    ///     Agent Bridge 实例运行登记。
    /// </summary>
    [Serializable]
    public sealed class AgentBridgeInstanceRegistration
    {
        /// <summary>
        ///     Unity 实例标识。
        /// </summary>
        public string InstanceId;

        /// <summary>
        ///     Unity 进程 Id。
        /// </summary>
        public int ProcessId;

        /// <summary>
        ///     工程根目录。
        /// </summary>
        public string ProjectPath;

        /// <summary>
        ///     当前实例绑定的 Host。
        /// </summary>
        public string Host;

        /// <summary>
        ///     当前实例绑定的 Port。
        /// </summary>
        public int? Port;

        /// <summary>
        ///     当前实例是否运行中。
        /// </summary>
        public bool IsRunning;

        /// <summary>
        ///     最近一次状态更新时间（UTC ISO-8601）。
        /// </summary>
        public string LastSeenUtc;
    }
}
