using UnityEngine;
using xFrame.Runtime.ObjectPool;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI视图基类
    /// 所有UI组件的基类，提供统一的生命周期管理
    /// </summary>
    public abstract class UIView : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// UI所在的层级
        /// </summary>
        public virtual UILayer Layer { get; protected set; } = UILayer.Normal;

        /// <summary>
        /// UI是否已创建（OnCreate已调用）
        /// </summary>
        public bool IsCreated { get; internal set; }

        /// <summary>
        /// UI是否已打开
        /// </summary>
        public bool IsOpen { get; internal set; }

        /// <summary>
        /// UI的Canvas组件
        /// </summary>
        public Canvas Canvas { get; private set; }

        /// <summary>
        /// UI的CanvasGroup组件（用于控制可见性和交互）
        /// </summary>
        public CanvasGroup CanvasGroup { get; private set; }

        /// <summary>
        /// UI的RectTransform
        /// </summary>
        public RectTransform RectTransform { get; private set; }

        /// <summary>
        /// 是否允许缓存到对象池
        /// </summary>
        public virtual bool Cacheable => true;

        /// <summary>
        /// 组件管理器
        /// </summary>
        public UIComponentManager ComponentManager { get; private set; }

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            CanvasGroup = GetComponent<CanvasGroup>();
            RectTransform = GetComponent<RectTransform>();

            // 如果没有CanvasGroup组件，自动添加
            if (CanvasGroup == null) CanvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 初始化组件管理器
            ComponentManager = new UIComponentManager();
        }

        #region 生命周期回调

        /// <summary>
        /// UI创建时调用（仅调用一次）
        /// 用于初始化UI组件、绑定事件等
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// UI打开时调用（可多次调用）
        /// </summary>
        /// <param name="data">传递给UI的数据</param>
        protected virtual void OnOpen(object data)
        {
        }

        /// <summary>
        /// UI显示时调用（可多次调用）
        /// 当UI从隐藏状态变为显示状态时调用
        /// 场景：1. 打开时调用 2. 从导航栈中恢复时调用
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// UI隐藏时调用（可多次调用）
        /// 当UI从显示状态变为隐藏状态时调用
        /// 场景：1. 被其他UI遮挡压入栈时调用 2. 关闭前调用
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// UI关闭时调用（可多次调用）
        /// </summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>
        /// UI销毁时调用（仅调用一次）
        /// 用于清理资源、取消事件订阅等
        /// </summary>
        protected virtual void OnUIDestroy()
        {
        }

        #endregion

        #region IPoolable接口实现

        /// <summary>
        /// 从对象池获取时调用
        /// </summary>
        public void OnGet()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 释放回对象池时调用
        /// </summary>
        public void OnRelease()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 对象被销毁时调用（IPoolable接口）
        /// </summary>
        public void OnDestroy()
        {
            // 调用 UI 销毁生命周期
            InternalOnDestroy();
        }

        #endregion

        #region 内部方法（由UIManager调用）

        /// <summary>
        /// 内部：调用创建生命周期
        /// </summary>
        internal void InternalOnCreate()
        {
            if (!IsCreated)
            {
                OnCreate();
                IsCreated = true;
            }
        }

        /// <summary>
        /// 内部：调用打开生命周期
        /// </summary>
        internal void InternalOnOpen(object data)
        {
            if (!IsOpen)
            {
                OnOpen(data);
                IsOpen = true;

                // 打开后自动调用Show
                InternalOnShow();
            }
        }

        /// <summary>
        /// 内部：调用显示生命周期
        /// </summary>
        internal void InternalOnShow()
        {
            OnShow();

            // 显示UI
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);

            // 通知子组件父UI已显示
            ComponentManager?.OnParentShow();
        }

        /// <summary>
        /// 内部：调用隐藏生命周期
        /// </summary>
        internal void InternalOnHide()
        {
            OnHide();

            // 隐藏UI
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }

            // 注意：不设置SetActive(false)，因为UI可能只是被遮挡，而非真正关闭

            // 通知子组件父UI已隐藏
            ComponentManager?.OnParentHide();
        }

        /// <summary>
        /// 内部：调用关闭生命周期
        /// </summary>
        internal void InternalOnClose()
        {
            if (IsOpen)
            {
                // 关闭前先隐藏
                InternalOnHide();

                OnClose();
                IsOpen = false;

                gameObject.SetActive(false);

                // 通知子组件父UI已关闭
                ComponentManager?.OnParentClose();
            }
        }

        /// <summary>
        /// 内部：调用销毁生命周期
        /// </summary>
        internal void InternalOnDestroy()
        {
            if (IsCreated)
            {
                OnUIDestroy();
                IsCreated = false;

                // 销毁所有子组件
                ComponentManager?.OnParentDestroy();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置UI的可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            if (CanvasGroup != null) CanvasGroup.alpha = visible ? 1f : 0f;
        }

        /// <summary>
        /// 设置UI的交互性
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
}