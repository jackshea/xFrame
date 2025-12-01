using System;
using System.Collections.Generic;
using UnityEngine;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI组件管理器
    /// 用于管理UIView下的所有子组件
    /// 依赖关系：父组件通过管理器管理子组件，子组件不知道父组件
    /// </summary>
    public class UIComponentManager
    {
        private readonly Dictionary<string, UIComponent> _components = new Dictionary<string, UIComponent>();
        private readonly Dictionary<Type, List<UIComponent>> _componentsByType = new Dictionary<Type, List<UIComponent>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public UIComponentManager()
        {
        }

        #region 组件注册和管理

        /// <summary>
        /// 注册组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        public void RegisterComponent<T>(T component) where T : UIComponent
        {
            if (component == null)
            {
                Debug.LogError("[UIComponentManager] 组件为空，无法注册");
                return;
            }

            // 初始化组件
            if (!component.IsInitialized)
            {
                component.Initialize();
            }

            // 按ID存储
            if (!_components.ContainsKey(component.ComponentId))
            {
                _components[component.ComponentId] = component;
            }

            // 按类型存储
            var componentType = typeof(T);
            if (!_componentsByType.ContainsKey(componentType))
            {
                _componentsByType[componentType] = new List<UIComponent>();
            }

            if (!_componentsByType[componentType].Contains(component))
            {
                _componentsByType[componentType].Add(component);
            }

            Debug.Log($"[UIComponentManager] 注册组件: {componentType.Name}, ID: {component.ComponentId}");
        }

        /// <summary>
        /// 自动查找并注册所有子组件
        /// </summary>
        /// <param name="rootTransform">根节点</param>
        public void AutoRegisterComponents(Transform rootTransform)
        {
            if (rootTransform == null)
            {
                Debug.LogError("[UIComponentManager] rootTransform为空");
                return;
            }

            var components = rootTransform.GetComponentsInChildren<UIComponent>(true);
            foreach (var component in components)
            {
                // 跳过已注册的
                if (component.IsInitialized) continue;

                component.Initialize();
                _components[component.ComponentId] = component;

                var componentType = component.GetType();
                if (!_componentsByType.ContainsKey(componentType))
                {
                    _componentsByType[componentType] = new List<UIComponent>();
                }

                if (!_componentsByType[componentType].Contains(component))
                {
                    _componentsByType[componentType].Add(component);
                }
            }

            Debug.Log($"[UIComponentManager] 自动注册了 {components.Length} 个组件");
        }

        /// <summary>
        /// 注销组件
        /// </summary>
        /// <param name="componentId">组件ID</param>
        public void UnregisterComponent(string componentId)
        {
            if (_components.TryGetValue(componentId, out var component))
            {
                _components.Remove(componentId);

                var componentType = component.GetType();
                if (_componentsByType.TryGetValue(componentType, out var list))
                {
                    list.Remove(component);
                }

                component.DestroyComponent();
                Debug.Log($"[UIComponentManager] 注销组件: {componentType.Name}, ID: {componentId}");
            }
        }

        #endregion

        #region 组件查询

        /// <summary>
        /// 通过ID获取组件
        /// </summary>
        /// <param name="componentId">组件ID</param>
        /// <returns>组件实例</returns>
        public UIComponent GetComponent(string componentId)
        {
            _components.TryGetValue(componentId, out var component);
            return component;
        }

        /// <summary>
        /// 通过ID获取组件（泛型）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="componentId">组件ID</param>
        /// <returns>组件实例</returns>
        public T GetComponent<T>(string componentId) where T : UIComponent
        {
            return GetComponent(componentId) as T;
        }

        /// <summary>
        /// 获取指定类型的第一个组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        public T GetComponentOfType<T>() where T : UIComponent
        {
            var componentType = typeof(T);
            if (_componentsByType.TryGetValue(componentType, out var list) && list.Count > 0)
            {
                return list[0] as T;
            }
            return null;
        }

        /// <summary>
        /// 获取指定类型的所有组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件列表</returns>
        public List<T> GetComponentsOfType<T>() where T : UIComponent
        {
            var result = new List<T>();
            var componentType = typeof(T);

            if (_componentsByType.TryGetValue(componentType, out var list))
            {
                foreach (var component in list)
                {
                    if (component is T typedComponent)
                    {
                        result.Add(typedComponent);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <returns>所有组件列表</returns>
        public List<UIComponent> GetAllComponents()
        {
            return new List<UIComponent>(_components.Values);
        }

        #endregion

        #region 生命周期传递

        /// <summary>
        /// 当父UI显示时，通知所有可见的子组件
        /// </summary>
        public void OnParentShow()
        {
            foreach (var component in _components.Values)
            {
                if (component.IsVisible)
                {
                    component.Show();
                }
            }
        }

        /// <summary>
        /// 当父UI隐藏时，通知所有子组件
        /// </summary>
        public void OnParentHide()
        {
            foreach (var component in _components.Values)
            {
                if (component.IsVisible)
                {
                    component.Hide();
                }
            }
        }

        /// <summary>
        /// 当父UI关闭时，重置所有子组件
        /// </summary>
        public void OnParentClose()
        {
            foreach (var component in _components.Values)
            {
                component.Reset();
            }
        }

        /// <summary>
        /// 当父UI销毁时，销毁所有子组件
        /// </summary>
        public void OnParentDestroy()
        {
            foreach (var component in _components.Values)
            {
                component.DestroyComponent();
            }

            _components.Clear();
            _componentsByType.Clear();
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 显示所有组件
        /// </summary>
        public void ShowAll()
        {
            foreach (var component in _components.Values)
            {
                component.Show();
            }
        }

        /// <summary>
        /// 隐藏所有组件
        /// </summary>
        public void HideAll()
        {
            foreach (var component in _components.Values)
            {
                component.Hide();
            }
        }

        /// <summary>
        /// 显示指定类型的所有组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void ShowComponentsOfType<T>() where T : UIComponent
        {
            var components = GetComponentsOfType<T>();
            foreach (var component in components)
            {
                component.Show();
            }
        }

        /// <summary>
        /// 隐藏指定类型的所有组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void HideComponentsOfType<T>() where T : UIComponent
        {
            var components = GetComponentsOfType<T>();
            foreach (var component in components)
            {
                component.Hide();
            }
        }

        /// <summary>
        /// 刷新所有组件
        /// </summary>
        public void RefreshAll()
        {
            foreach (var component in _components.Values)
            {
                component.Refresh();
            }
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取组件总数
        /// </summary>
        /// <returns>组件数量</returns>
        public int GetComponentCount()
        {
            return _components.Count;
        }

        /// <summary>
        /// 获取指定类型的组件数量
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件数量</returns>
        public int GetComponentCountOfType<T>() where T : UIComponent
        {
            var componentType = typeof(T);
            if (_componentsByType.TryGetValue(componentType, out var list))
            {
                return list.Count;
            }
            return 0;
        }

        #endregion
    }
}
