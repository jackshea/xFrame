using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using xFrame.Config;

namespace xFrame.Editor.Config
{
    /// <summary>
    ///     配置管理窗口，用于集中管理项目中的所有配置资源。
    /// </summary>
    public class ConfigManagerWindow : EditorWindow
    {
        private readonly List<BaseConfig> _configs = new();
        private Vector2 _scrollPosition;
        private string _searchString = "";

        private void OnEnable()
        {
            RefreshConfigs();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawConfigList();
            DrawFooter();
        }

        private void OnFocus()
        {
            RefreshConfigs();
        }

        [MenuItem("xFrame/Config/Config Manager")]
        public static void ShowWindow()
        {
            GetWindow<ConfigManagerWindow>("Config Manager");
        }

        private void RefreshConfigs()
        {
            _configs.Clear();
            var guids = AssetDatabase.FindAssets("t:BaseConfig");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<BaseConfig>(path);
                if (config != null) _configs.Add(config);
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) RefreshConfigs();
            GUILayout.FlexibleSpace();
            _searchString = EditorGUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
        }

        private void DrawConfigList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_configs.Count == 0)
                GUILayout.Label("No configuration files found.", EditorStyles.centeredGreyMiniLabel);

            foreach (var config in _configs)
            {
                // 搜索时匹配：资产名称、标题、备注
                if (!string.IsNullOrEmpty(_searchString))
                {
                    var search = _searchString.ToLower();
                    var matchName = config.name.ToLower().Contains(search);
                    var matchTitle = !string.IsNullOrEmpty(config.Title) && config.Title.ToLower().Contains(search);
                    var matchDesc = !string.IsNullOrEmpty(config.Description) &&
                                    config.Description.ToLower().Contains(search);
                    if (!matchName && !matchTitle && !matchDesc)
                        continue;
                }

                EditorGUILayout.BeginHorizontal("box");

                // 图标
                GUILayout.Label(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUILayout.Width(20),
                    GUILayout.Height(20));

                // 名称、标题和类型
                GUILayout.BeginVertical();
                var displayName = string.IsNullOrEmpty(config.Title) ? config.name : $"{config.name} - {config.Title}";
                GUILayout.Label(displayName, EditorStyles.boldLabel);
                GUILayout.Label(config.GetType().Name, EditorStyles.miniLabel);
                // 显示备注（如果有）
                if (!string.IsNullOrEmpty(config.Description))
                    GUILayout.Label(config.Description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = config;
                    EditorGUIUtility.PingObject(config);
                }

                if (GUILayout.Button("Edit", GUILayout.Width(50))) AssetDatabase.OpenAsset(config);

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void DrawFooter()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Tip: Create new configs by inheriting from BaseConfig and adding [CreateAssetMenu].",
                EditorStyles.helpBox);
            GUILayout.EndVertical();
        }
    }
}