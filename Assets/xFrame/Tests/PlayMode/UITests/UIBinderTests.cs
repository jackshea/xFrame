using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using xFrame.Runtime.UI;

namespace xFrame.Tests.PlayMode.UITests
{
    /// <summary>
    /// UIBinder绑定辅助工具的PlayMode测试
    /// 需要在PlayMode下测试，因为涉及Unity组件的交互
    /// </summary>
    [TestFixture]
    public class UIBinderTests
    {
        private GameObject _testRoot;

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

            // 创建Toggle
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(panel.transform);
            toggleGO.AddComponent<Toggle>();

            // 创建Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(panel.transform);
            sliderGO.AddComponent<Slider>();

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

            // 创建深层嵌套
            var deepPanel = new GameObject("DeepPanel");
            deepPanel.transform.SetParent(subPanel.transform);

            var deepButton = new GameObject("DeepButton");
            deepButton.transform.SetParent(deepPanel.transform);
            deepButton.AddComponent<Button>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                Object.Destroy(_testRoot);
            }
        }

        #region FindChild测试

        /// <summary>
        /// 测试查找直接子对象
        /// </summary>
        [Test]
        public void FindChild_DirectChild_ShouldReturnTransform()
        {
            var result = UIBinder.FindChild(_testRoot.transform, "Panel");
            Assert.IsNotNull(result, "应找到Panel");
            Assert.AreEqual("Panel", result.name);
        }

        /// <summary>
        /// 测试查找嵌套路径
        /// </summary>
        [Test]
        public void FindChild_NestedPath_ShouldReturnTransform()
        {
            var result = UIBinder.FindChild(_testRoot.transform, "Panel/Button");
            Assert.IsNotNull(result, "应找到Panel/Button");
            Assert.AreEqual("Button", result.name);
        }

        /// <summary>
        /// 测试查找不存在的路径
        /// </summary>
        [Test]
        public void FindChild_NonExistingPath_ShouldReturnNull()
        {
            var result = UIBinder.FindChild(_testRoot.transform, "NonExisting");
            Assert.IsNull(result, "不存在的路径应返回null");
        }

        /// <summary>
        /// 测试null根节点
        /// </summary>
        [Test]
        public void FindChild_NullRoot_ShouldReturnNull()
        {
            Transform nullRoot = null;
            var result = UIBinder.FindChild(nullRoot, "Panel");
            Assert.IsNull(result, "null根节点应返回null");
        }

        /// <summary>
        /// 测试空路径
        /// </summary>
        [Test]
        public void FindChild_EmptyPath_ShouldReturnNull()
        {
            var result = UIBinder.FindChild(_testRoot.transform, "");
            Assert.IsNull(result, "空路径应返回null");
        }

        /// <summary>
        /// 测试null路径
        /// </summary>
        [Test]
        public void FindChild_NullPath_ShouldReturnNull()
        {
            var result = UIBinder.FindChild(_testRoot.transform, null);
            Assert.IsNull(result, "null路径应返回null");
        }

        #endregion

        #region FindComponent测试

        /// <summary>
        /// 测试查找组件
        /// </summary>
        [Test]
        public void FindComponent_ExistingComponent_ShouldReturnComponent()
        {
            var button = _testRoot.transform.FindComponent<Button>("Panel/Button");
            Assert.IsNotNull(button, "应找到Button组件");
        }

        /// <summary>
        /// 测试查找不存在的组件
        /// </summary>
        [Test]
        public void FindComponent_NonExistingPath_ShouldReturnNull()
        {
            var button = _testRoot.transform.FindComponent<Button>("Panel/NonExisting");
            Assert.IsNull(button, "不存在的路径应返回null");
        }

        /// <summary>
        /// 测试查找错误类型的组件
        /// </summary>
        [Test]
        public void FindComponent_WrongType_ShouldReturnNull()
        {
            var image = _testRoot.transform.FindComponent<Image>("Panel/Button");
            Assert.IsNull(image, "错误类型应返回null");
        }

        #endregion

        #region FindChildRecursive测试

        /// <summary>
        /// 测试递归查找直接子对象
        /// </summary>
        [Test]
        public void FindChildRecursive_DirectChild_ShouldReturnTransform()
        {
            var result = _testRoot.transform.FindChildRecursive("Panel");
            Assert.IsNotNull(result, "应找到Panel");
        }

        /// <summary>
        /// 测试递归查找深层子对象
        /// </summary>
        [Test]
        public void FindChildRecursive_DeepChild_ShouldReturnTransform()
        {
            var result = _testRoot.transform.FindChildRecursive("DeepButton");
            Assert.IsNotNull(result, "应找到DeepButton");
            Assert.AreEqual("DeepButton", result.name);
        }

        /// <summary>
        /// 测试递归查找不存在的对象
        /// </summary>
        [Test]
        public void FindChildRecursive_NonExisting_ShouldReturnNull()
        {
            var result = _testRoot.transform.FindChildRecursive("NonExisting");
            Assert.IsNull(result, "不存在的对象应返回null");
        }

        /// <summary>
        /// 测试递归查找null根节点
        /// </summary>
        [Test]
        public void FindChildRecursive_NullRoot_ShouldReturnNull()
        {
            Transform nullRoot = null;
            var result = nullRoot.FindChildRecursive("Button");
            Assert.IsNull(result, "null根节点应返回null");
        }

        /// <summary>
        /// 测试递归查找空名称
        /// </summary>
        [Test]
        public void FindChildRecursive_EmptyName_ShouldReturnNull()
        {
            var result = _testRoot.transform.FindChildRecursive("");
            Assert.IsNull(result, "空名称应返回null");
        }

        #endregion

        #region FindComponentRecursive测试

        /// <summary>
        /// 测试递归查找组件
        /// </summary>
        [Test]
        public void FindComponentRecursive_ExistingComponent_ShouldReturnComponent()
        {
            var button = _testRoot.transform.FindComponentRecursive<Button>("DeepButton");
            Assert.IsNotNull(button, "应找到DeepButton上的Button组件");
        }

        /// <summary>
        /// 测试递归查找不存在的组件
        /// </summary>
        [Test]
        public void FindComponentRecursive_NonExisting_ShouldReturnNull()
        {
            var button = _testRoot.transform.FindComponentRecursive<Button>("NonExisting");
            Assert.IsNull(button, "不存在的对象应返回null");
        }

        #endregion

        #region BindButton测试

        /// <summary>
        /// 测试绑定Button点击事件
        /// </summary>
        [UnityTest]
        public IEnumerator BindButton_ShouldBindClickEvent()
        {
            var clicked = false;
            var button = _testRoot.transform.BindButton("Panel/Button", () => clicked = true);

            Assert.IsNotNull(button, "应返回Button组件");

            // 模拟点击
            button.onClick.Invoke();
            yield return null;

            Assert.IsTrue(clicked, "点击回调应被触发");
        }

        /// <summary>
        /// 测试绑定Button到不存在的路径
        /// </summary>
        [Test]
        public void BindButton_NonExistingPath_ShouldReturnNull()
        {
            var button = _testRoot.transform.BindButton("NonExisting", () => { });
            Assert.IsNull(button, "不存在的路径应返回null");
        }

        /// <summary>
        /// 测试绑定Button时回调为null
        /// </summary>
        [Test]
        public void BindButton_NullCallback_ShouldReturnButton()
        {
            var button = _testRoot.transform.BindButton("Panel/Button", null);
            Assert.IsNotNull(button, "即使回调为null也应返回Button");
        }

        #endregion

        #region BindToggle测试

        /// <summary>
        /// 测试绑定Toggle值改变事件
        /// </summary>
        [UnityTest]
        public IEnumerator BindToggle_ShouldBindValueChangedEvent()
        {
            var receivedValue = false;
            var toggle = _testRoot.transform.BindToggle("Panel/Toggle", value => receivedValue = value);

            Assert.IsNotNull(toggle, "应返回Toggle组件");

            // 模拟值改变
            toggle.isOn = true;
            yield return null;

            Assert.IsTrue(receivedValue, "值改变回调应被触发");
        }

        #endregion

        #region BindSlider测试

        /// <summary>
        /// 测试绑定Slider值改变事件
        /// </summary>
        [UnityTest]
        public IEnumerator BindSlider_ShouldBindValueChangedEvent()
        {
            var receivedValue = 0f;
            var slider = _testRoot.transform.BindSlider("Panel/Slider", value => receivedValue = value);

            Assert.IsNotNull(slider, "应返回Slider组件");

            // 模拟值改变
            slider.value = 0.5f;
            yield return null;

            Assert.AreEqual(0.5f, receivedValue, 0.001f, "值改变回调应被触发");
        }

        #endregion

        #region SetText测试

        /// <summary>
        /// 测试设置文本
        /// </summary>
        [Test]
        public void SetText_ValidPath_ShouldSetText()
        {
            var text = _testRoot.transform.SetText("Panel/Text", "Hello World");

            Assert.IsNotNull(text, "应返回Text组件");
            Assert.AreEqual("Hello World", text.text, "文本应被设置");
        }

        /// <summary>
        /// 测试设置文本到不存在的路径
        /// </summary>
        [Test]
        public void SetText_NonExistingPath_ShouldReturnNull()
        {
            var text = _testRoot.transform.SetText("NonExisting", "Hello");
            Assert.IsNull(text, "不存在的路径应返回null");
        }

        #endregion

        #region GetText测试

        /// <summary>
        /// 测试获取文本
        /// </summary>
        [Test]
        public void GetText_ValidPath_ShouldReturnText()
        {
            // 先设置文本
            _testRoot.transform.SetText("Panel/Text", "Test Content");

            var result = _testRoot.transform.GetText("Panel/Text");

            Assert.AreEqual("Test Content", result, "应返回正确的文本");
        }

        /// <summary>
        /// 测试获取不存在路径的文本
        /// </summary>
        [Test]
        public void GetText_NonExistingPath_ShouldReturnEmpty()
        {
            var result = _testRoot.transform.GetText("NonExisting");
            Assert.AreEqual(string.Empty, result, "不存在的路径应返回空字符串");
        }

        #endregion

        #region SetSprite测试

        /// <summary>
        /// 测试设置图片
        /// </summary>
        [Test]
        public void SetSprite_ValidPath_ShouldSetSprite()
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var image = _testRoot.transform.SetSprite("Panel/Image", sprite);

            Assert.IsNotNull(image, "应返回Image组件");
            Assert.AreEqual(sprite, image.sprite, "Sprite应被设置");

            // 清理
            Object.Destroy(sprite);
        }

        /// <summary>
        /// 测试设置图片到不存在的路径
        /// </summary>
        [Test]
        public void SetSprite_NonExistingPath_ShouldReturnNull()
        {
            var image = _testRoot.transform.SetSprite("NonExisting", null);
            Assert.IsNull(image, "不存在的路径应返回null");
        }

        #endregion

        #region SetColor测试

        /// <summary>
        /// 测试设置颜色
        /// </summary>
        [Test]
        public void SetColor_ValidPath_ShouldSetColor()
        {
            var color = Color.red;
            var image = _testRoot.transform.SetColor("Panel/Image", color);

            Assert.IsNotNull(image, "应返回Image组件");
            Assert.AreEqual(color, image.color, "颜色应被设置");
        }

        #endregion

        #region SetFillAmount测试

        /// <summary>
        /// 测试设置填充量
        /// </summary>
        [Test]
        public void SetFillAmount_ValidPath_ShouldSetFillAmount()
        {
            var image = _testRoot.transform.SetFillAmount("Panel/Image", 0.5f);

            Assert.IsNotNull(image, "应返回Image组件");
            Assert.AreEqual(0.5f, image.fillAmount, 0.001f, "填充量应被设置");
        }

        #endregion

        #region SetActive测试

        /// <summary>
        /// 测试设置对象可见性
        /// </summary>
        [Test]
        public void SetActive_ValidPath_ShouldSetVisibility()
        {
            _testRoot.transform.SetActive("Panel/Button", false);
            var button = _testRoot.transform.Find("Panel/Button");

            Assert.IsFalse(button.gameObject.activeSelf, "对象应被隐藏");

            _testRoot.transform.SetActive("Panel/Button", true);
            Assert.IsTrue(button.gameObject.activeSelf, "对象应被显示");
        }

        /// <summary>
        /// 测试设置不存在路径的可见性
        /// </summary>
        [Test]
        public void SetActive_NonExistingPath_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _testRoot.transform.SetActive("NonExisting", false));
        }

        #endregion

        #region SetCanvasGroupVisible测试

        /// <summary>
        /// 测试设置CanvasGroup可见性
        /// </summary>
        [Test]
        public void SetCanvasGroupVisible_ValidPath_ShouldSetVisibility()
        {
            _testRoot.transform.SetCanvasGroupVisible("Panel/Button", false);
            var button = _testRoot.transform.Find("Panel/Button");
            var canvasGroup = button.GetComponent<CanvasGroup>();

            Assert.IsNotNull(canvasGroup, "应自动添加CanvasGroup");
            Assert.AreEqual(0f, canvasGroup.alpha, "alpha应为0");
            Assert.IsFalse(canvasGroup.interactable, "应不可交互");
            Assert.IsFalse(canvasGroup.blocksRaycasts, "应不阻挡射线");
        }

        /// <summary>
        /// 测试设置CanvasGroup可见性为true
        /// </summary>
        [Test]
        public void SetCanvasGroupVisible_True_ShouldSetVisible()
        {
            _testRoot.transform.SetCanvasGroupVisible("Panel/Button", true);
            var button = _testRoot.transform.Find("Panel/Button");
            var canvasGroup = button.GetComponent<CanvasGroup>();

            Assert.AreEqual(1f, canvasGroup.alpha, "alpha应为1");
            Assert.IsTrue(canvasGroup.interactable, "应可交互");
            Assert.IsTrue(canvasGroup.blocksRaycasts, "应阻挡射线");
        }

        /// <summary>
        /// 测试设置CanvasGroup可见但不可交互
        /// </summary>
        [Test]
        public void SetCanvasGroupVisible_VisibleNotInteractable_ShouldWork()
        {
            _testRoot.transform.SetCanvasGroupVisible("Panel/Button", true, false);
            var button = _testRoot.transform.Find("Panel/Button");
            var canvasGroup = button.GetComponent<CanvasGroup>();

            Assert.AreEqual(1f, canvasGroup.alpha, "alpha应为1");
            Assert.IsFalse(canvasGroup.interactable, "应不可交互");
            Assert.IsFalse(canvasGroup.blocksRaycasts, "应不阻挡射线");
        }

        #endregion
    }
}
