using System.Collections.Generic;
using UnityEngine;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI标签页容器构建器
    /// 提供流式API来构建UITabContainer
    /// </summary>
    public class UITabContainerBuilder
    {
        private readonly UITabContainer _container;
        private readonly List<TabPageConfig> _pageConfigs = new();
        private Transform _buttonContainer;
        private UITabButton _buttonPrefab;
        private Transform _pageContainer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="container">容器实例</param>
        public UITabContainerBuilder(UITabContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// 设置页面容器
        /// </summary>
        /// <param name="pageContainer">页面父节点</param>
        /// <returns>Builder实例</returns>
        public UITabContainerBuilder WithPageContainer(Transform pageContainer)
        {
            _pageContainer = pageContainer;
            return this;
        }

        /// <summary>
        /// 设置按钮容器
        /// </summary>
        /// <param name="buttonContainer">按钮父节点</param>
        /// <returns>Builder实例</returns>
        public UITabContainerBuilder WithButtonContainer(Transform buttonContainer)
        {
            _buttonContainer = buttonContainer;
            return this;
        }

        /// <summary>
        /// 设置按钮预制体
        /// </summary>
        /// <param name="buttonPrefab">按钮预制体</param>
        /// <returns>Builder实例</returns>
        public UITabContainerBuilder WithButtonPrefab(UITabButton buttonPrefab)
        {
            _buttonPrefab = buttonPrefab;
            return this;
        }

        /// <summary>
        /// 添加页面
        /// </summary>
        /// <param name="page">页面实例</param>
        /// <param name="buttonText">按钮文本（可选）</param>
        /// <param name="buttonIcon">按钮图标（可选）</param>
        /// <returns>Builder实例</returns>
        public UITabContainerBuilder AddPage(UITabPage page, string buttonText = null, Sprite buttonIcon = null)
        {
            _pageConfigs.Add(new TabPageConfig
            {
                Page = page,
                ButtonText = buttonText ?? page.PageName,
                ButtonIcon = buttonIcon
            });
            return this;
        }

        /// <summary>
        /// 添加页面（泛型）
        /// </summary>
        /// <typeparam name="T">页面类型</typeparam>
        /// <param name="pagePrefab">页面预制体</param>
        /// <param name="buttonText">按钮文本（可选）</param>
        /// <param name="buttonIcon">按钮图标（可选）</param>
        /// <returns>Builder实例</returns>
        public UITabContainerBuilder AddPage<T>(T pagePrefab, string buttonText = null, Sprite buttonIcon = null)
            where T : UITabPage
        {
            if (pagePrefab == null)
            {
                Debug.LogError("[UITabContainerBuilder] 页面预制体为空");
                return this;
            }

            // 实例化页面
            var pageInstance = GameObject.Instantiate(pagePrefab);

            return AddPage(pageInstance, buttonText, buttonIcon);
        }

        /// <summary>
        /// 构建容器
        /// </summary>
        /// <returns>构建好的容器</returns>
        public UITabContainer Build()
        {
            if (_container == null)
            {
                Debug.LogError("[UITabContainerBuilder] 容器为空");
                return null;
            }

            // 添加所有页面
            for (var i = 0; i < _pageConfigs.Count; i++)
            {
                var config = _pageConfigs[i];
                var pageIndex = _container.AddPage(config.Page);

                // 创建按钮（如果配置了）
                if (_buttonContainer != null && _buttonPrefab != null)
                    CreateButton(pageIndex, config.ButtonText, config.ButtonIcon);
            }

            return _container;
        }

        /// <summary>
        /// 创建标签按钮
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="buttonText">按钮文本</param>
        /// <param name="buttonIcon">按钮图标</param>
        private void CreateButton(int pageIndex, string buttonText, Sprite buttonIcon)
        {
            // 实例化按钮
            var button = GameObject.Instantiate(_buttonPrefab, _buttonContainer);

            // 注册到组件管理器
            _container.ComponentManager.RegisterComponent(button);

            // 设置按钮数据
            button.SetData(new TabButtonData
            {
                PageIndex = pageIndex,
                Text = buttonText,
                Icon = buttonIcon,
                Container = _container
            });

            button.Show();

            // 监听页面切换事件，更新按钮状态
            _container.OnPageChanged += (oldIndex, newIndex) => { button.SetSelected(newIndex == pageIndex); };
        }

        /// <summary>
        /// 页面配置
        /// </summary>
        private class TabPageConfig
        {
            public UITabPage Page { get; set; }
            public string ButtonText { get; set; }
            public Sprite ButtonIcon { get; set; }
        }
    }

    /// <summary>
    /// UITabContainer扩展方法
    /// </summary>
    public static class UITabContainerExtensions
    {
        /// <summary>
        /// 创建构建器
        /// </summary>
        /// <param name="container">容器实例</param>
        /// <returns>Builder实例</returns>
        public static UITabContainerBuilder CreateBuilder(this UITabContainer container)
        {
            return new UITabContainerBuilder(container);
        }
    }
}