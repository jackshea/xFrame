using UnityEngine;

namespace xFrame.Runtime
{
    /// <summary>
    ///     旧启动入口兼容工具。
    ///     负责识别新旧入口共存场景，并给出统一告警，降低误接入概率。
    /// </summary>
    public static class LegacyStartupCompatibility
    {
        private static readonly string[] LegacyEntryTypeNames =
        {
            typeof(xFrameApplication).FullName,
            typeof(xFrameBootstrapper).FullName
        };

        /// <summary>
        ///     创建旧入口兼容告警文案。
        /// </summary>
        public static string CreateLegacyEntryWarning(string legacyEntryName)
        {
            return
                $"检测到场景中已存在 UnityStartupEntry，{legacyEntryName} 仅作为兼容层保留，建议改用 UnityStartupEntry 作为唯一启动入口。";
        }

        /// <summary>
        ///     创建新入口与旧入口共存时的统一告警文案。
        /// </summary>
        public static string CreateModernEntryWarning(string modernEntryName, string legacyEntryName)
        {
            return
                $"检测到场景中已存在旧启动入口 {legacyEntryName}，{modernEntryName} 应作为唯一启动入口，请移除旧入口以避免启动链路并存。";
        }

        /// <summary>
        ///     判断当前已加载场景中是否存在新的启动入口。
        /// </summary>
        public static bool HasModernStartupEntryInLoadedScenes()
        {
            var modernEntryType = typeof(xFrameApplication).Assembly.GetType("xFrame.Runtime.Unity.Startup.UnityStartupEntry");
            return HasEntryInLoadedScenes(modernEntryType);
        }

        /// <summary>
        ///     获取当前已加载场景中的旧启动入口名称。
        /// </summary>
        public static string GetLegacyStartupEntryNameInLoadedScenes()
        {
            var assembly = typeof(xFrameApplication).Assembly;
            for (var i = 0; i < LegacyEntryTypeNames.Length; i++)
            {
                var legacyEntryType = assembly.GetType(LegacyEntryTypeNames[i]);
                if (legacyEntryType == null)
                    continue;

                if (HasEntryInLoadedScenes(legacyEntryType))
                    return legacyEntryType.Name;
            }

            return null;
        }

        /// <summary>
        ///     当检测到新旧入口共存时输出统一告警。
        /// </summary>
        public static void WarnIfModernStartupEntryExists(string legacyEntryName)
        {
            if (!HasModernStartupEntryInLoadedScenes())
                return;

            Debug.LogWarning(CreateLegacyEntryWarning(legacyEntryName));
        }

        /// <summary>
        ///     当检测到旧启动入口时输出统一告警。
        /// </summary>
        public static void WarnIfLegacyStartupEntryExists(string modernEntryName)
        {
            var legacyEntryName = GetLegacyStartupEntryNameInLoadedScenes();
            if (string.IsNullOrWhiteSpace(legacyEntryName))
                return;

            Debug.LogWarning(CreateModernEntryWarning(modernEntryName, legacyEntryName));
        }

        private static bool HasEntryInLoadedScenes(System.Type entryType)
        {
            if (entryType == null)
                return false;

            var entries = Resources.FindObjectsOfTypeAll(entryType);
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] is not Component component)
                    continue;

                if (component.gameObject.scene.IsValid())
                    return true;
            }

            return false;
        }
    }
}
