using System;
using UnityEngine;

namespace xFrame.Config
{
    /// <summary>
    /// 配置对象的基类，继承自 ScriptableObject。
    /// 支持运行时修改数据并发出变更事件。
    /// </summary>
    public abstract class BaseConfig : ScriptableObject
    {
        [Header("配置信息 / Config Info")]
        [Tooltip("配置的标题，用于描述此配置的名称或用途")]
        [SerializeField]
        private string _title = "";

        [Tooltip("配置的备注，用于详细说明此配置的作用")]
        [TextArea(2, 5)]
        [SerializeField]
        private string _description = "";

        /// <summary>
        /// 配置的标题。
        /// </summary>
        public string Title => _title;

        /// <summary>
        /// 配置的备注/描述。
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 当配置数据发生变化时触发的事件。
        /// </summary>
        public event Action<BaseConfig> OnConfigChanged;

        /// <summary>
        /// Unity 编辑器回调，当 Inspector 中的值被修改时调用。
        /// 用于在运行时实时响应配置修改。
        /// </summary>
        protected virtual void OnValidate()
        {
            // 仅在运行时通知，避免编辑器下的不必要触发，或者根据需求调整
            if (Application.isPlaying)
            {
                NotifyConfigChanged();
            }
        }

        /// <summary>
        /// 手动通知配置已更改。
        /// </summary>
        public void NotifyConfigChanged()
        {
            OnConfigChanged?.Invoke(this);
            Debug.Log($"[Config] {name} has been updated.");
        }
    }
}
