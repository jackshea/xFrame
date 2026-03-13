using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using xFrame.Runtime;
using xFrame.Runtime.Unity.Startup;

namespace xFrame.Editor.Bootstrapper
{
    /// <summary>
    ///     旧启动入口场景守卫。
    ///     在编辑器打开或保存场景时检查新旧入口是否共存，并输出统一告警。
    /// </summary>
    [InitializeOnLoad]
    public static class LegacyStartupSceneGuard
    {
        static LegacyStartupSceneGuard()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ValidateScene(scene);
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            if (!ContainsComponent<UnityStartupEntry>(scene))
                return;

            var legacyEntryNames = new List<string>();
            if (ContainsComponent<xFrameBootstrapper>(scene))
                legacyEntryNames.Add(nameof(xFrameBootstrapper));

            if (ContainsComponent<xFrameApplication>(scene))
                legacyEntryNames.Add(nameof(xFrameApplication));

            for (var i = 0; i < legacyEntryNames.Count; i++)
                Debug.LogWarning(
                    $"{LegacyStartupCompatibility.CreateLegacyEntryWarning(legacyEntryNames[i])} Scene={scene.path}");
        }

        private static bool ContainsComponent<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                if (roots[i].GetComponentInChildren<T>(true) != null)
                    return true;

            return false;
        }
    }
}
