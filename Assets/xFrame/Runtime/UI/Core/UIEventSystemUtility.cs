using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

namespace xFrame.Runtime.UI
{
    /// <summary>
    ///     UI 事件系统辅助工具。
    ///     负责确保场景中存在可用的 EventSystem 与输入模块。
    /// </summary>
    public static class UIEventSystemUtility
    {
        /// <summary>
        ///     确保场景中存在可用的 EventSystem。
        /// </summary>
        /// <param name="parent">自动创建时的父节点。</param>
        /// <returns>可用的 EventSystem 实例。</returns>
        public static EventSystem EnsureEventSystem(Transform parent = null)
        {
            var eventSystem = Resources.FindObjectsOfTypeAll<EventSystem>()
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene.IsValid());

            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                if (parent != null) eventSystemGO.transform.SetParent(parent, false);

                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();

                Debug.Log("[UI] 自动创建EventSystem");
                return eventSystem;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[UI] 为现有EventSystem补充StandaloneInputModule");
            }

            return eventSystem;
        }
    }
}
