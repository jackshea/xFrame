using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using xFrame.Runtime.UI;

namespace xFrame.Tests.EditMode.UITests
{
    /// <summary>
    /// UIReferenceCache引用缓存的单元测试
    /// </summary>
    [TestFixture]
    public class UIReferenceCacheTests
    {
        private GameObject _testRoot;
        private UIReferenceCache _cache;

        [SetUp]
        public void SetUp()
        {
            // 创建测试UI层级结构
            _testRoot = new GameObject("TestRoot");
            
            // 创建Panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_testRoot.transform);
            
            // 创建Button
            var buttonGO = new GameObject("Button");
            buttonGO.transform.SetParent(panel.transform);
            buttonGO.AddComponent<Button>();
            
            // 创建Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(panel.transform);
            textGO.AddComponent<TextMeshProUGUI>();
            
            // 创建Image
            var imageGO = new GameObject("Image");
            imageGO.transform.SetParent(panel.transform);
            imageGO.AddComponent<Image>();
            
            // 创建嵌套结构
            var subPanel = new GameObject("SubPanel");
            subPanel.transform.SetParent(panel.transform);
            
            var nestedButton = new GameObject("NestedButton");
            nestedButton.transform.SetParent(subPanel.transform);
            nestedButton.AddComponent<Button>();
            
            _cache = new UIReferenceCache(_testRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.DestroyImmediate(_testRoot);
            }
        }

        #region 构造函数测试

        /// <summary>
        /// 测试构造函数
        /// </summary>
        [Test]
        public void Constructor_WithValidRoot_ShouldCreateCache()
        {
            var cache = new UIReferenceCache(_testRoot.transform);
            Assert.IsNotNull(cache, "缓存应被创建");
        }

        #endregion

        #region Get方法测试

        /// <summary>
        /// 测试获取存在的组件
        /// </summary>
        [Test]
        public void Get_ExistingComponent_ShouldReturnComponent()
        {
            var button = _cache.Get<Button>("Panel/Button");
            Assert.IsNotNull(button, "应返回Button组件");
        }

        /// <summary>
        /// 测试获取不存在的组件
        /// </summary>
        [Test]
        public void Get_NonExistingComponent_ShouldReturnNull()
        {
            var button = _cache.Get<Button>("Panel/NonExisting");
            Assert.IsNull(button, "不存在的组件应返回null");
        }

        /// <summary>
        /// 测试获取不存在路径的组件
        /// </summary>
        [Test]
        public void Get_NonExistingPath_ShouldReturnNull()
        {
            var button = _cache.Get<Button>("NonExisting/Path");
            Assert.IsNull(button, "不存在的路径应返回null");
        }

        /// <summary>
        /// 测试获取错误类型的组件
        /// </summary>
        [Test]
        public void Get_WrongComponentType_ShouldReturnNull()
        {
            // Panel/Button上有Button组件，但没有Image组件
            var image = _cache.Get<Image>("Panel/Button");
            Assert.IsNull(image, "错误类型应返回null");
        }

        /// <summary>
        /// 测试获取嵌套路径的组件
        /// </summary>
        [Test]
        public void Get_NestedPath_ShouldReturnComponent()
        {
            var button = _cache.Get<Button>("Panel/SubPanel/NestedButton");
            Assert.IsNotNull(button, "应返回嵌套路径的组件");
        }

        /// <summary>
        /// 测试获取不同类型的组件
        /// </summary>
        [Test]
        public void Get_DifferentTypes_ShouldReturnCorrectComponents()
        {
            var button = _cache.Get<Button>("Panel/Button");
            var text = _cache.Get<TextMeshProUGUI>("Panel/Text");
            var image = _cache.Get<Image>("Panel/Image");

            Assert.IsNotNull(button, "应返回Button");
            Assert.IsNotNull(text, "应返回Text");
            Assert.IsNotNull(image, "应返回Image");
        }

        #endregion

        #region 缓存功能测试

        /// <summary>
        /// 测试缓存命中
        /// </summary>
        [Test]
        public void Get_SamePathTwice_ShouldReturnCachedComponent()
        {
            var button1 = _cache.Get<Button>("Panel/Button");
            var button2 = _cache.Get<Button>("Panel/Button");

            Assert.AreSame(button1, button2, "应返回相同的缓存组件");
        }

        /// <summary>
        /// 测试不同类型同路径的缓存
        /// </summary>
        [Test]
        public void Get_SamePathDifferentTypes_ShouldCacheSeparately()
        {
            // 在Button上添加Image组件用于测试
            var buttonGO = _testRoot.transform.Find("Panel/Button");
            buttonGO.gameObject.AddComponent<Image>();

            var button = _cache.Get<Button>("Panel/Button");
            var image = _cache.Get<Image>("Panel/Button");

            Assert.IsNotNull(button, "应返回Button");
            Assert.IsNotNull(image, "应返回Image");
            Assert.AreNotSame(button, image, "不同类型应分别缓存");
        }

        #endregion

        #region Clear方法测试

        /// <summary>
        /// 测试清除缓存
        /// </summary>
        [Test]
        public void Clear_ShouldClearAllCache()
        {
            // 先缓存一些组件
            _cache.Get<Button>("Panel/Button");
            _cache.Get<TextMeshProUGUI>("Panel/Text");

            // 清除缓存
            _cache.Clear();

            // 再次获取应该重新查找（虽然结果相同，但这里主要测试Clear不会抛异常）
            var button = _cache.Get<Button>("Panel/Button");
            Assert.IsNotNull(button, "清除后应能重新获取组件");
        }

        /// <summary>
        /// 测试清除空缓存
        /// </summary>
        [Test]
        public void Clear_EmptyCache_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _cache.Clear(), "清除空缓存不应抛异常");
        }

        #endregion

        #region Remove方法测试

        /// <summary>
        /// 测试移除指定路径的缓存
        /// </summary>
        [Test]
        public void Remove_ExistingPath_ShouldRemoveFromCache()
        {
            // 先缓存组件
            var button1 = _cache.Get<Button>("Panel/Button");

            // 移除缓存
            _cache.Remove("Panel/Button");

            // 再次获取（应该重新查找，但结果相同）
            var button2 = _cache.Get<Button>("Panel/Button");
            Assert.IsNotNull(button2, "移除后应能重新获取组件");
        }

        /// <summary>
        /// 测试移除不存在路径的缓存
        /// </summary>
        [Test]
        public void Remove_NonExistingPath_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _cache.Remove("NonExisting/Path"), "移除不存在的路径不应抛异常");
        }

        /// <summary>
        /// 测试移除只影响指定路径
        /// </summary>
        [Test]
        public void Remove_ShouldOnlyRemoveSpecifiedPath()
        {
            // 缓存多个组件
            var button = _cache.Get<Button>("Panel/Button");
            var text = _cache.Get<TextMeshProUGUI>("Panel/Text");

            // 只移除Button的缓存
            _cache.Remove("Panel/Button");

            // Text的缓存应该还在
            var text2 = _cache.Get<TextMeshProUGUI>("Panel/Text");
            Assert.AreSame(text, text2, "未移除的路径应保持缓存");
        }

        #endregion

        #region 边界情况测试

        /// <summary>
        /// 测试空路径
        /// </summary>
        [Test]
        public void Get_EmptyPath_ShouldReturnNull()
        {
            var result = _cache.Get<Button>("");
            Assert.IsNull(result, "空路径应返回null");
        }

        /// <summary>
        /// 测试根节点组件
        /// </summary>
        [Test]
        public void Get_RootComponent_ShouldWork()
        {
            // 在根节点添加组件
            _testRoot.AddComponent<Image>();
            
            // 创建新的缓存指向根节点
            var rootCache = new UIReferenceCache(_testRoot.transform);
            
            // 直接获取Panel下的组件
            var button = rootCache.Get<Button>("Panel/Button");
            Assert.IsNotNull(button, "应能获取子节点组件");
        }

        #endregion
    }
}
