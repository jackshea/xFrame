using UnityEditor;
using UnityEngine;
using xFrame.Config;

namespace xFrame.Editor.Config
{
    /// <summary>
    ///     BaseConfig 的自定义编辑器。
    ///     在 Inspector 中提供额外的调试功能。
    /// </summary>
    [CustomEditor(typeof(BaseConfig), true)]
    public class ConfigInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var config = (BaseConfig)target;

            // 显示运行时状态提示
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("运行时模式：修改属性将自动触发事件。\nRuntime Mode: Changes will trigger events.",
                    MessageType.Info);

                // 手动触发事件的按钮，方便测试
                if (GUILayout.Button("强制触发变更事件 (Force Trigger Event)")) config.NotifyConfigChanged();
            }

            GUILayout.Space(5);

            // 绘制标题和备注区域（带样式）
            DrawConfigHeader(config);

            GUILayout.Space(5);

            // 绘制默认的 Inspector
            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///     绘制配置的标题和备注信息区域。
        /// </summary>
        private void DrawConfigHeader(BaseConfig config)
        {
            // 标题区域样式
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUILayout.BeginVertical("box");

            // 显示标题
            var title = string.IsNullOrEmpty(config.Title) ? config.name : config.Title;
            EditorGUILayout.LabelField("📋 " + title, headerStyle);

            // 显示备注
            if (!string.IsNullOrEmpty(config.Description))
                EditorGUILayout.HelpBox(config.Description, MessageType.None);

            EditorGUILayout.EndVertical();
        }
    }
}