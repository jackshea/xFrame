using UnityEngine;
using UnityEditor;
using xFrame.Config;
using System.Collections.Generic;

namespace xFrame.Editor.Config
{
    /// <summary>
    /// 配置管理窗口，用于集中管理项目中的所有配置资源。
    /// </summary>
    public class ConfigManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<BaseConfig> _configs = new List<BaseConfig>();
        private string _searchString = "";

        [MenuItem("xFrame/Config/Config Manager")]
        public static void ShowWindow()
        {
            GetWindow<ConfigManagerWindow>("Config Manager");
        }

        private void OnEnable()
        {
            RefreshConfigs();
        }

        private void OnFocus()
        {
            RefreshConfigs();
        }

        private void RefreshConfigs()
        {
            _configs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:BaseConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BaseConfig config = AssetDatabase.LoadAssetAtPath<BaseConfig>(path);
                if (config != null)
                {
                    _configs.Add(config);
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawConfigList();
            DrawFooter();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshConfigs();
            }
            GUILayout.FlexibleSpace();
            _searchString = EditorGUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
        }

        private void DrawConfigList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_configs.Count == 0)
            {
                GUILayout.Label("No configuration files found.", EditorStyles.centeredGreyMiniLabel);
            }

            foreach (var config in _configs)
            {
                // 搜索时匹配：资产名称、标题、备注
                if (!string.IsNullOrEmpty(_searchString))
                {
                    string search = _searchString.ToLower();
                    bool matchName = config.name.ToLower().Contains(search);
                    bool matchTitle = !string.IsNullOrEmpty(config.Title) && config.Title.ToLower().Contains(search);
                    bool matchDesc = !string.IsNullOrEmpty(config.Description) && config.Description.ToLower().Contains(search);
                    if (!matchName && !matchTitle && !matchDesc)
                        continue;
                }

                EditorGUILayout.BeginHorizontal("box");
                
                // 图标
                GUILayout.Label(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUILayout.Width(20), GUILayout.Height(20));
                
                // 名称、标题和类型
                GUILayout.BeginVertical();
                string displayName = string.IsNullOrEmpty(config.Title) ? config.name : $"{config.name} - {config.Title}";
                GUILayout.Label(displayName, EditorStyles.boldLabel);
                GUILayout.Label(config.GetType().Name, EditorStyles.miniLabel);
                // 显示备注（如果有）
                if (!string.IsNullOrEmpty(config.Description))
                {
                    GUILayout.Label(config.Description, EditorStyles.wordWrappedMiniLabel);
                }
                GUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = config;
                    EditorGUIUtility.PingObject(config);
                }
                
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    AssetDatabase.OpenAsset(config);
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void DrawFooter()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Tip: Create new configs by inheriting from BaseConfig and adding [CreateAssetMenu].", EditorStyles.helpBox);
            GUILayout.EndVertical();
        }
    }
}
