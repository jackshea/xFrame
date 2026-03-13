using UnityEngine;

namespace xFrame.Runtime
{
    /// <summary>
    ///     旧启动入口兼容工具。
    ///     负责识别新旧入口共存场景，并给出统一告警，降低误接入概率。
    /// </summary>
    public static class LegacyStartupCompatibility
    {
        /// <summary>
        ///     创建旧入口兼容告警文案。
        /// </summary>
        public static string CreateLegacyEntryWarning(string legacyEntryName)
        {
            return
                $"检测到场景中已存在 UnityStartupEntry，{legacyEntryName} 仅作为兼容层保留，建议改用 UnityStartupEntry 作为唯一启动入口。";
        }

        /// <summary>
        ///     判断当前已加载场景中是否存在新的启动入口。
        /// </summary>
        public static bool HasModernStartupEntryInLoadedScenes()
        {
            var modernEntryType = typeof(xFrameApplication).Assembly.GetType("xFrame.Runtime.Unity.Startup.UnityStartupEntry");
            if (modernEntryType == null)
                return false;

            var modernEntries = Resources.FindObjectsOfTypeAll(modernEntryType);
            for (var i = 0; i < modernEntries.Length; i++)
            {
                if (modernEntries[i] is not Component component)
                    continue;

                if (component.gameObject.scene.IsValid())
                    return true;
            }

            return false;
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
    }
}
