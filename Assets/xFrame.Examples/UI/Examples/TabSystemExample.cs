using UnityEngine;
using xFrame.Runtime.UI;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// Tab系统使用示例
    /// 演示如何使用UITabContainer和UITabPage实现多页面切换
    /// </summary>
    public class TabSystemExample : MonoBehaviour
    {
        [Header("示例1：基础Tab容器")]
        [SerializeField] private UITabContainer basicTabContainer;
        [SerializeField] private HomePage homePagePrefab;
        [SerializeField] private InventoryPage inventoryPagePrefab;
        [SerializeField] private ShopPage shopPagePrefab;
        
        [Header("示例2：带按钮的Tab容器")]
        [SerializeField] private UITabContainer advancedTabContainer;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private UITabButton tabButtonPrefab;
        
        private void Start()
        {
            // 示例1：基础用法
            Example1_BasicTabContainer();
            
            // 示例2：使用Builder模式
            Example2_BuilderPattern();
            
            // 示例3：子页面独立使用
            Example3_StandalonePageUsage();
        }
        
        /// <summary>
        /// 示例1：基础Tab容器使用
        /// </summary>
        private void Example1_BasicTabContainer()
        {
            Debug.Log("=== 示例1：基础Tab容器 ===");
            
            // 创建容器实例
            var container = Instantiate(basicTabContainer);
            
            // 添加页面
            var homePage = Instantiate(homePagePrefab);
            var inventoryPage = Instantiate(inventoryPagePrefab);
            var shopPage = Instantiate(shopPagePrefab);
            
            container.AddPage(homePage);
            container.AddPage(inventoryPage);
            container.AddPage(shopPage);
            
            // 打开容器
            container.InternalOnCreate();
            container.InternalOnOpen(null);
            
            // 切换页面
            container.SwitchPage(0); // 切换到首页
            container.SwitchPage<InventoryPage>(); // 通过类型切换
            container.SwitchPage("ShopPage"); // 通过名称切换
            
            // 下一页/上一页
            container.NextPage();
            container.PreviousPage();
        }
        
        /// <summary>
        /// 示例2：使用Builder模式构建容器
        /// </summary>
        private void Example2_BuilderPattern()
        {
            Debug.Log("=== 示例2：Builder模式 ===");
            
            // 使用Builder模式构建容器
            var container = Instantiate(advancedTabContainer);
            
            container.CreateBuilder()
                .WithButtonContainer(buttonContainer)
                .WithButtonPrefab(tabButtonPrefab)
                .AddPage(Instantiate(homePagePrefab), "首页")
                .AddPage(Instantiate(inventoryPagePrefab), "背包")
                .AddPage(Instantiate(shopPagePrefab), "商店")
                .Build();
            
            // 监听页面切换事件
            container.OnPageChanged += (oldIndex, newIndex) =>
            {
                Debug.Log($"页面切换: {oldIndex} -> {newIndex}");
            };
            
            // 打开容器
            container.InternalOnCreate();
            container.InternalOnOpen(null);
        }
        
        /// <summary>
        /// 示例3：子页面独立使用
        /// 演示TabPage既可以在容器中使用，也可以独立使用
        /// </summary>
        private void Example3_StandalonePageUsage()
        {
            Debug.Log("=== 示例3：子页面独立使用 ===");
            
            // 方式1：作为容器的一部分
            var container = Instantiate(basicTabContainer);
            var pageInContainer = Instantiate(homePagePrefab);
            container.AddPage(pageInContainer);
            container.InternalOnCreate();
            container.InternalOnOpen(null);
            
            Debug.Log($"页面在容器中: IsInContainer={pageInContainer.IsInContainer}");
            
            // 方式2：作为独立页面使用
            var standalonePage = Instantiate(homePagePrefab);
            standalonePage.InternalOnCreate();
            standalonePage.InternalOnOpen(null);
            
            Debug.Log($"独立页面: IsInContainer={standalonePage.IsInContainer}");
            
            // 对于页面来说，这两种使用方式是无感知的
            // 页面的生命周期回调都会正常调用
        }
    }
    
    #region 示例页面实现
    
    /// <summary>
    /// 首页示例
    /// </summary>
    public class HomePage : UITabPage
    {
        public override string PageName => "HomePage";
        
        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("[HomePage] OnCreate - 页面创建");
        }
        
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            Debug.Log("[HomePage] OnOpen - 页面打开");
        }
        
        protected override void OnPageEnter()
        {
            base.OnPageEnter();
            Debug.Log("[HomePage] OnPageEnter - 进入页面");
        }
        
        protected override void OnPageExit()
        {
            base.OnPageExit();
            Debug.Log("[HomePage] OnPageExit - 退出页面");
        }
        
        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("[HomePage] OnShow - 页面显示");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("[HomePage] OnHide - 页面隐藏");
        }
    }
    
    /// <summary>
    /// 背包页面示例
    /// </summary>
    public class InventoryPage : UITabPage
    {
        public override string PageName => "InventoryPage";
        
        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("[InventoryPage] OnCreate");
        }
        
        protected override void OnPageEnter()
        {
            base.OnPageEnter();
            Debug.Log("[InventoryPage] OnPageEnter - 刷新背包数据");
            
            // 进入页面时刷新数据
            RefreshInventoryData();
        }
        
        private void RefreshInventoryData()
        {
            // 刷新背包数据逻辑
            Debug.Log("[InventoryPage] 刷新背包数据");
        }
    }
    
    /// <summary>
    /// 商店页面示例
    /// </summary>
    public class ShopPage : UITabPage
    {
        public override string PageName => "ShopPage";
        
        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("[ShopPage] OnCreate");
        }
        
        protected override void OnPageEnter()
        {
            base.OnPageEnter();
            Debug.Log("[ShopPage] OnPageEnter - 加载商品列表");
            
            // 进入页面时加载商品
            LoadShopItems();
        }
        
        private void LoadShopItems()
        {
            // 加载商品列表逻辑
            Debug.Log("[ShopPage] 加载商品列表");
        }
    }
    
    #endregion
}
