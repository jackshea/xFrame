using UnityEngine;
using VContainer.Unity;
using xFrame.Core.DI;

namespace xFrame.Core
{
    /// <summary>
    /// 模块系统启动器
    /// 负责在游戏开始时初始化整个模块系统
    /// </summary>
    public class ModuleBootstrap : MonoBehaviour
    {
        /// <summary>
        /// 生命周期容器预制体
        /// </summary>
        [SerializeField] 
        private LifetimeScope lifetimeScopePrefab;

        /// <summary>
        /// Unity Awake生命周期方法
        /// </summary>
        private void Awake()
        {
            Debug.Log("模块系统启动器初始化中...");
            
            // 如果没有指定预制体，尝试查找场景中的LifetimeScope
            if (lifetimeScopePrefab == null)
            {
                var existingScope = FindObjectOfType<xFrameLifetimeScope>();
                if (existingScope == null)
                {
                    Debug.LogWarning("未找到模块生命周期容器，创建默认容器");
                    var scopeObj = new GameObject("xFrameLifetimeScope");
                    scopeObj.AddComponent<xFrameLifetimeScope>();
                }
                else
                {
                    Debug.Log($"使用场景中已有的生命周期容器: {existingScope.name}");
                }
            }
            else
            {
                // 如果已经指定了预制体，直接实例化
                var instance = Instantiate(lifetimeScopePrefab);
                Debug.Log($"已实例化模块生命周期容器预制体: {instance.name}");
            }
            
            Debug.Log("模块系统启动器初始化完成");
        }
    }
}
