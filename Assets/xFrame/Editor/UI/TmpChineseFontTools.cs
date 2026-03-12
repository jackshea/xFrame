using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace xFrame.Editor.UI
{
    /// <summary>
    ///     为项目生成并应用中文 TMP 字体资源。
    /// </summary>
    public static class TmpChineseFontTools
    {
        private const string SourceFontPath = "Assets/Fonts/NotoSansSC-Regular.ttf";
        private const string FontAssetPath = "Assets/Fonts/NotoSansSC-Regular SDF.asset";
        private const string AutoApplySessionKey = "xFrame.Editor.UI.NotoSansSc.AutoApply";

        /// <summary>
        ///     编辑器域重载完成后自动补齐中文 TMP 字体。
        /// </summary>
        [InitializeOnLoadMethod]
        public static void AutoApplyNotoSansScFont()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(AutoApplySessionKey, false))
                {
                    return;
                }

                SessionState.SetBool(AutoApplySessionKey, true);

                if (!AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath))
                {
                    return;
                }

                if (!NeedsApply())
                {
                    return;
                }

                ApplyNotoSansScFont();
            };
        }

        /// <summary>
        ///     生成中文 TMP 字体资源，并将当前场景中的 TMP 文本切换到该字体。
        /// </summary>
        [MenuItem("xFrame/UI/Apply NotoSansSC TMP Font")]
        public static void ApplyNotoSansScFont()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"未找到源字体文件: {SourceFontPath}");
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (IsFontAssetBroken(fontAsset))
            {
                DeleteFontAssetFilePreserveMeta();
                fontAsset = null;
            }

            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                fontAsset.name = "NotoSansSC-Regular SDF";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                EnsureFontAssetSubAssets(fontAsset);
            }

            EnsureFallbackFont(fontAsset);
            ApplyFontToScene(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"已应用中文 TMP 字体: {FontAssetPath}");
        }

        /// <summary>
        ///     判断当前工程是否仍需应用中文 TMP 字体。
        /// </summary>
        /// <returns>若仍需生成或切换字体则返回 true。</returns>
        private static bool NeedsApply()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (IsFontAssetBroken(fontAsset))
            {
                return true;
            }

            var texts = Object.FindObjectsOfType<TMP_Text>(true);
            return texts.Any(text => text != null && text.font != fontAsset);
        }

        /// <summary>
        ///     判断 TMP 字体资源是否已损坏，避免存在空壳资源时跳过修复。
        /// </summary>
        /// <param name="fontAsset">待校验的字体资源。</param>
        /// <returns>若字体材质或图集丢失则返回 true。</returns>
        private static bool IsFontAssetBroken(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return true;
            }

            var atlasTextures = fontAsset.atlasTextures;
            var hasAtlasTexture = atlasTextures != null && atlasTextures.Any(texture => texture != null);
            return fontAsset.material == null || !hasAtlasTexture;
        }

        /// <summary>
        ///     删除损坏的字体资源文件并保留 meta，保证引用 GUID 不变。
        /// </summary>
        private static void DeleteFontAssetFilePreserveMeta()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("无法定位 Unity 项目根目录，跳过损坏字体资源清理。");
                return;
            }

            var absoluteAssetPath = Path.Combine(projectRoot, FontAssetPath);
            if (!File.Exists(absoluteAssetPath))
            {
                return;
            }

            File.Delete(absoluteAssetPath);
        }

        /// <summary>
        ///     将 TMP 动态字体运行时生成的图集与材质补存为子资源，避免落盘后变成空壳资产。
        /// </summary>
        /// <param name="fontAsset">刚创建的字体资源。</param>
        private static void EnsureFontAssetSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            var atlasTexture = fontAsset.atlasTexture;
            if (atlasTexture != null && !AssetDatabase.Contains(atlasTexture))
            {
                atlasTexture.name = $"{fontAsset.name} Atlas";
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = $"{fontAsset.name} Atlas Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
        }

        /// <summary>
        ///     将生成的中文字体添加到 TMP 全局回退字体列表。
        /// </summary>
        /// <param name="fontAsset">目标中文字体资源。</param>
        private static void EnsureFallbackFont(TMP_FontAsset fontAsset)
        {
            if (TMP_Settings.instance == null)
            {
                return;
            }

            var fallbackFonts = TMP_Settings.fallbackFontAssets;
            if (fallbackFonts != null && fallbackFonts.Contains(fontAsset))
            {
                EditorUtility.SetDirty(TMP_Settings.instance);
                return;
            }

            fallbackFonts?.Add(fontAsset);
            EditorUtility.SetDirty(TMP_Settings.instance);
        }

        /// <summary>
        ///     将当前已加载场景中的所有 TMP 文本组件切换为中文字体。
        /// </summary>
        /// <param name="fontAsset">目标中文字体资源。</param>
        private static void ApplyFontToScene(TMP_FontAsset fontAsset)
        {
            var texts = Object.FindObjectsOfType<TMP_Text>(true);
            foreach (var text in texts.Where(text => text != null))
            {
                text.font = fontAsset;
                EditorUtility.SetDirty(text);
            }
        }
    }
}
