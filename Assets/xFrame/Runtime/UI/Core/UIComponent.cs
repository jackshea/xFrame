using System;
using UnityEngine;
using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI组件基类
    /// 可复用的UI子组件，支持父子关系管理和生命周期传递
    /// 依赖关系：父组件知道子组件，子组件不知道父组件
    /// </summary>
    public abstract class UIComponent : MonoBehaviour
    {
        /// <summary>
        /// 组件唯一ID（自动生成）
        /// 用于区分同类型的多个组件实例
        /// </summary>
        public string ComponentId { get; private set; }

        /// <summary>
        /// 组件是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 组件是否可见
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 组件的CanvasGroup（用于控制可见性）
        /// </summary>
        protected CanvasGroup CanvasGroup { get; private set; }

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        protected virtual void Awake()
        {
            // 生成唯一ID
            ComponentId = $"{GetType().Name}_{Guid.NewGuid():N}";

            // 获取或添加CanvasGroup
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null) CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        #region 组件事件系统

        /// <summary>
        /// 发送组件事件（通过事件总线）
        /// 事件会自动携带组件ID，父UI可以通过订阅事件并根据ComponentId来识别是哪个子组件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="componentEvent">组件事件</param>
        protected void SendEvent<T>(T componentEvent) where T : struct, IUIComponentEvent
        {
            // 包装为带组件ID的事件
            var wrappedEvent = new UIComponentEventWrapper<T>
            {
                ComponentId = ComponentId,
                ComponentType = GetType(),
                SourceComponent = this,
                Event = componentEvent
            };

            // 通过事件总线发送全局事件
            xFrameEventBus.Raise(wrappedEvent);

            Debug.Log($"[{GetType().Name}] 发送事件: {typeof(T).Name}, ComponentId: {ComponentId}");
        }

        #endregion

        #region 组件生命周期

        /// <summary>
        /// 初始化组件
        /// 由父UI调用，只执行一次
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            OnInitialize();
            IsInitialized = true;
        }

        /// <summary>
        /// 组件初始化回调
        /// 用于绑定事件、获取组件引用等
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 设置组件数据
        /// 可多次调用
        /// </summary>
        /// <param name="data">组件数据</param>
        public void SetData(object data)
        {
            OnSetData(data);
        }

        /// <summary>
        /// 设置数据回调
        /// </summary>
        /// <param name="data">组件数据</param>
        protected virtual void OnSetData(object data)
        {
        }

        /// <summary>
        /// 显示组件
        /// </summary>
        public void Show()
        {
            if (IsVisible) return;

            IsVisible = true;
            OnShow();

            // 显示UI
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 显示回调
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 隐藏组件
        /// </summary>
        public void Hide()
        {
            if (!IsVisible) return;

            IsVisible = false;
            OnHide();

            // 隐藏UI
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 隐藏回调
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 刷新组件显示
        /// </summary>
        public void Refresh()
        {
            OnRefresh();
        }

        /// <summary>
        /// 刷新回调
        /// </summary>
        protected virtual void OnRefresh()
        {
        }

        /// <summary>
        /// 重置组件状态
        /// 用于对象池回收前
        /// </summary>
        public void Reset()
        {
            OnReset();
            IsVisible = false;
        }

        /// <summary>
        /// 重置回调
        /// </summary>
        protected virtual void OnReset()
        {
        }

        /// <summary>
        /// 销毁组件
        /// </summary>
        public void DestroyComponent()
        {
            OnDestroyComponent();
            IsInitialized = false;
        }

        /// <summary>
        /// 销毁回调
        /// </summary>
        protected virtual void OnDestroyComponent()
        {
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置组件可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            if (visible)
                Show();
            else
                Hide();
        }

        /// <summary>
        /// 设置组件交互性
        /// </summary>
        /// <param name="interactable">是否可交互</param>
        public void SetInteractable(bool interactable)
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.interactable = interactable;
                CanvasGroup.blocksRaycasts = interactable;
            }
        }

        #endregion
    }

    /// <summary>
    /// 泛型UI组件基类
    /// 提供类型安全的数据设置方法
    /// </summary>
    /// <typeparam name="TData">组件数据类型</typeparam>
    public abstract class UIComponent<TData> : UIComponent where TData : class
    {
        /// <summary>
        /// 当前组件数据
        /// </summary>
        protected TData CurrentData { get; private set; }

        /// <summary>
        /// 类型安全的数据设置方法
        /// </summary>
        /// <param name="data">组件数据</param>
        public void SetData(TData data)
        {
            CurrentData = data;
            OnSetData(data);
        }

        /// <summary>
        /// 重写基类的SetData，提供类型转换
        /// </summary>
        /// <param name="data">组件数据</param>
        protected sealed override void OnSetData(object data)
        {
            if (data is TData typedData)
            {
                CurrentData = typedData;
                OnSetData(typedData);
            }
            else if (data != null)
            {
                Debug.LogWarning($"[{GetType().Name}] 数据类型不匹配: 期望 {typeof(TData).Name}, 实际 {data.GetType().Name}");
            }
        }

        /// <summary>
        /// 类型安全的数据设置回调
        /// 子类重写此方法处理数据
        /// </summary>
        /// <param name="data">组件数据</param>
        protected virtual void OnSetData(TData data)
        {
        }

        /// <summary>
        /// 获取当前数据
        /// </summary>
        /// <returns>当前组件数据</returns>
        public TData GetData()
        {
            return CurrentData;
        }

        /// <summary>
        /// 检查是否有数据
        /// </summary>
        /// <returns>是否有数据</returns>
        public bool HasData()
        {
            return CurrentData != null;
        }

        /// <summary>
        /// 重置时清空数据
        /// </summary>
        protected override void OnReset()
        {
            base.OnReset();
            CurrentData = null;
        }
    }

    #region 组件事件接口和类型

    /// <summary>
    /// UI组件事件接口
    /// 所有组件事件都需要实现此接口
    /// </summary>
    public interface IUIComponentEvent : IEvent
    {
    }

    /// <summary>
    /// UI组件事件包装器
    /// 自动包含组件ID和来源信息
    /// </summary>
    /// <typeparam name="T">实际事件类型</typeparam>
    public struct UIComponentEventWrapper<T> : IEvent where T : struct, IUIComponentEvent
    {
        /// <summary>
        /// 组件ID（唯一标识）
        /// </summary>
        public string ComponentId { get; set; }

        /// <summary>
        /// 组件类型
        /// </summary>
        public Type ComponentType { get; set; }

        /// <summary>
        /// 源组件实例
        /// </summary>
        public UIComponent SourceComponent { get; set; }

        /// <summary>
        /// 实际事件数据
        /// </summary>
        public T Event { get; set; }
    }

    #endregion
}