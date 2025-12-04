using System;
using System.Collections.Generic;
using UnityEngine;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI标签页容器
    /// 使用组合模式管理多个UITabPage，支持页面切换
    /// 子页面既可以独立打开，也可以作为容器的一部分（对子页面无感）
    /// </summary>
    public class UITabContainer : UIPanel
    {
        [Header("标签页容器配置")]
        [SerializeField]
        [Tooltip("页面容器（所有子页面的父节点）")]
        private Transform pageContainer;
        
        [SerializeField]
        [Tooltip("默认激活的页面索引")]
        private int defaultPageIndex = 0;
        
        /// <summary>
        /// 所有子页面列表
        /// </summary>
        private readonly List<UITabPage> _pages = new List<UITabPage>();
        
        /// <summary>
        /// 页面索引映射（通过类型查找）
        /// </summary>
        private readonly Dictionary<Type, UITabPage> _pagesByType = new Dictionary<Type, UITabPage>();
        
        /// <summary>
        /// 页面索引映射（通过名称查找）
        /// </summary>
        private readonly Dictionary<string, UITabPage> _pagesByName = new Dictionary<string, UITabPage>();
        
        /// <summary>
        /// 当前激活的页面索引
        /// </summary>
        public int CurrentPageIndex { get; private set; } = -1;
        
        /// <summary>
        /// 当前激活的页面
        /// </summary>
        public UITabPage CurrentPage => CurrentPageIndex >= 0 && CurrentPageIndex < _pages.Count 
            ? _pages[CurrentPageIndex] 
            : null;
        
        /// <summary>
        /// 页面总数
        /// </summary>
        public int PageCount => _pages.Count;
        
        /// <summary>
        /// 页面切换事件
        /// </summary>
        public event Action<int, int> OnPageChanged; // (oldIndex, newIndex)
        
        /// <summary>
        /// 初始化容器
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            
            // 如果没有指定页面容器，使用自身
            if (pageContainer == null)
            {
                pageContainer = transform;
            }
        }
        
        #region 页面管理
        
        /// <summary>
        /// 添加页面
        /// </summary>
        /// <typeparam name="T">页面类型</typeparam>
        /// <param name="page">页面实例</param>
        /// <returns>页面索引</returns>
        public int AddPage<T>(T page) where T : UITabPage
        {
            if (page == null)
            {
                Debug.LogError("[UITabContainer] 添加页面失败：页面为空");
                return -1;
            }
            
            // 设置页面父节点
            page.transform.SetParent(pageContainer, false);
            
            // 标记页面在容器中
            page.IsInContainer = true;
            page.ParentContainer = this;
            page.PageIndex = _pages.Count;
            
            // 添加到列表
            _pages.Add(page);
            
            // 添加到类型映射
            var pageType = typeof(T);
            if (!_pagesByType.ContainsKey(pageType))
            {
                _pagesByType[pageType] = page;
            }
            
            // 添加到名称映射
            _pagesByName[page.PageName] = page;
            
            // 初始化页面（如果还未创建）
            if (!page.IsCreated)
            {
                page.InternalOnCreate();
            }
            
            // 默认隐藏页面
            page.InternalOnHide();
            
            Debug.Log($"[UITabContainer] 添加页面: {page.PageName}, 索引: {page.PageIndex}");
            
            return page.PageIndex;
        }
        
        /// <summary>
        /// 移除页面
        /// </summary>
        /// <param name="index">页面索引</param>
        public void RemovePage(int index)
        {
            if (index < 0 || index >= _pages.Count)
            {
                Debug.LogError($"[UITabContainer] 移除页面失败：索引越界 {index}");
                return;
            }
            
            var page = _pages[index];
            
            // 如果是当前页面，先切换到其他页面
            if (CurrentPageIndex == index)
            {
                int newIndex = index > 0 ? index - 1 : (index < _pages.Count - 1 ? index + 1 : -1);
                if (newIndex >= 0)
                {
                    SwitchPage(newIndex);
                }
                else
                {
                    CurrentPageIndex = -1;
                }
            }
            
            // 清理页面
            page.IsInContainer = false;
            page.ParentContainer = null;
            page.InternalOnClose();
            
            // 从映射中移除
            _pagesByType.Remove(page.GetType());
            _pagesByName.Remove(page.PageName);
            
            // 从列表中移除
            _pages.RemoveAt(index);
            
            // 更新后续页面的索引
            for (int i = index; i < _pages.Count; i++)
            {
                _pages[i].PageIndex = i;
            }
            
            Debug.Log($"[UITabContainer] 移除页面: {page.PageName}");
        }
        
        /// <summary>
        /// 获取页面
        /// </summary>
        /// <param name="index">页面索引</param>
        /// <returns>页面实例</returns>
        public UITabPage GetPage(int index)
        {
            if (index >= 0 && index < _pages.Count)
            {
                return _pages[index];
            }
            return null;
        }
        
        /// <summary>
        /// 获取页面（通过类型）
        /// </summary>
        /// <typeparam name="T">页面类型</typeparam>
        /// <returns>页面实例</returns>
        public T GetPage<T>() where T : UITabPage
        {
            if (_pagesByType.TryGetValue(typeof(T), out var page))
            {
                return page as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取页面（通过名称）
        /// </summary>
        /// <param name="pageName">页面名称</param>
        /// <returns>页面实例</returns>
        public UITabPage GetPage(string pageName)
        {
            if (_pagesByName.TryGetValue(pageName, out var page))
            {
                return page;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有页面
        /// </summary>
        /// <returns>页面列表</returns>
        public List<UITabPage> GetAllPages()
        {
            return new List<UITabPage>(_pages);
        }
        
        #endregion
        
        #region 页面切换
        
        /// <summary>
        /// 切换到指定页面
        /// </summary>
        /// <param name="index">页面索引</param>
        /// <param name="data">传递给页面的数据（可选）</param>
        public void SwitchPage(int index, object data = null)
        {
            if (index < 0 || index >= _pages.Count)
            {
                Debug.LogError($"[UITabContainer] 切换页面失败：索引越界 {index}");
                return;
            }
            
            if (CurrentPageIndex == index)
            {
                Debug.Log($"[UITabContainer] 已经是当前页面: {index}");
                return;
            }
            
            int oldIndex = CurrentPageIndex;
            var newPage = _pages[index];
            
            // 隐藏旧页面
            if (CurrentPageIndex >= 0 && CurrentPageIndex < _pages.Count)
            {
                var oldPage = _pages[CurrentPageIndex];
                oldPage.InternalPageExit();
                oldPage.InternalOnHide();
            }
            
            // 显示新页面
            CurrentPageIndex = index;
            
            // 打开新页面（如果还未打开）
            if (!newPage.IsOpen)
            {
                newPage.InternalOnOpen(data);
            }
            
            // 显示新页面
            newPage.InternalOnShow();
            newPage.InternalPageEnter();
            
            Debug.Log($"[UITabContainer] 切换页面: {oldIndex} -> {index} ({newPage.PageName})");
            
            // 触发事件
            OnPageChanged?.Invoke(oldIndex, index);
        }
        
        /// <summary>
        /// 切换到指定页面（通过类型）
        /// </summary>
        /// <typeparam name="T">页面类型</typeparam>
        /// <param name="data">传递给页面的数据（可选）</param>
        public void SwitchPage<T>(object data = null) where T : UITabPage
        {
            if (_pagesByType.TryGetValue(typeof(T), out var page))
            {
                SwitchPage(page.PageIndex, data);
            }
            else
            {
                Debug.LogError($"[UITabContainer] 切换页面失败：未找到类型 {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// 切换到指定页面（通过名称）
        /// </summary>
        /// <param name="pageName">页面名称</param>
        /// <param name="data">传递给页面的数据（可选）</param>
        public void SwitchPage(string pageName, object data = null)
        {
            if (_pagesByName.TryGetValue(pageName, out var page))
            {
                SwitchPage(page.PageIndex, data);
            }
            else
            {
                Debug.LogError($"[UITabContainer] 切换页面失败：未找到页面 {pageName}");
            }
        }
        
        /// <summary>
        /// 切换到下一个页面
        /// </summary>
        public void NextPage()
        {
            if (_pages.Count == 0) return;
            
            int nextIndex = (CurrentPageIndex + 1) % _pages.Count;
            SwitchPage(nextIndex);
        }
        
        /// <summary>
        /// 切换到上一个页面
        /// </summary>
        public void PreviousPage()
        {
            if (_pages.Count == 0) return;
            
            int prevIndex = CurrentPageIndex - 1;
            if (prevIndex < 0) prevIndex = _pages.Count - 1;
            SwitchPage(prevIndex);
        }
        
        #endregion
        
        #region 生命周期管理
        
        /// <summary>
        /// 容器创建时
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // 子类可以在这里动态创建页面
        }
        
        /// <summary>
        /// 容器打开时
        /// </summary>
        /// <param name="data">数据</param>
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            
            // 激活默认页面
            if (_pages.Count > 0)
            {
                int startIndex = Mathf.Clamp(defaultPageIndex, 0, _pages.Count - 1);
                SwitchPage(startIndex, data);
            }
        }
        
        /// <summary>
        /// 容器显示时
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            
            // 显示当前页面
            if (CurrentPage != null)
            {
                CurrentPage.InternalOnShow();
            }
        }
        
        /// <summary>
        /// 容器隐藏时
        /// </summary>
        protected override void OnHide()
        {
            base.OnHide();
            
            // 隐藏当前页面
            if (CurrentPage != null)
            {
                CurrentPage.InternalOnHide();
            }
        }
        
        /// <summary>
        /// 容器关闭时
        /// </summary>
        protected override void OnClose()
        {
            // 关闭所有页面
            foreach (var page in _pages)
            {
                if (page.IsOpen)
                {
                    page.InternalOnClose();
                }
            }
            
            CurrentPageIndex = -1;
            
            base.OnClose();
        }
        
        /// <summary>
        /// 容器销毁时
        /// </summary>
        protected override void OnUIDestroy()
        {
            // 销毁所有页面
            foreach (var page in _pages)
            {
                page.IsInContainer = false;
                page.ParentContainer = null;
                
                if (page.IsCreated)
                {
                    page.InternalOnDestroy();
                }
            }
            
            _pages.Clear();
            _pagesByType.Clear();
            _pagesByName.Clear();
            
            base.OnDestroy();
        }
        
        #endregion
    }
}
