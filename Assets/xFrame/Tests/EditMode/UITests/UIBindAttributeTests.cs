using NUnit.Framework;
using xFrame.Runtime.UI;

namespace xFrame.Tests.EditMode.UITests
{
    /// <summary>
    /// UIBindAttribute特性的单元测试
    /// </summary>
    [TestFixture]
    public class UIBindAttributeTests
    {
        #region 构造函数测试

        /// <summary>
        /// 测试无参数构造函数
        /// </summary>
        [Test]
        public void UIBindAttribute_DefaultConstructor_PathShouldBeNull()
        {
            var attr = new UIBindAttribute();
            Assert.IsNull(attr.Path, "默认构造时Path应为null");
        }

        /// <summary>
        /// 测试带路径参数的构造函数
        /// </summary>
        [Test]
        public void UIBindAttribute_WithPath_ShouldSetPath()
        {
            var path = "Panel/Button";
            var attr = new UIBindAttribute(path);
            Assert.AreEqual(path, attr.Path, "Path应正确设置");
        }

        /// <summary>
        /// 测试空字符串路径
        /// </summary>
        [Test]
        public void UIBindAttribute_EmptyPath_ShouldSetEmptyString()
        {
            var attr = new UIBindAttribute("");
            Assert.AreEqual("", attr.Path, "空字符串路径应被保留");
        }

        /// <summary>
        /// 测试null路径参数
        /// </summary>
        [Test]
        public void UIBindAttribute_NullPath_ShouldSetNull()
        {
            var attr = new UIBindAttribute(null);
            Assert.IsNull(attr.Path, "null路径应被保留");
        }

        #endregion

        #region 路径格式测试

        /// <summary>
        /// 测试简单路径
        /// </summary>
        [Test]
        public void UIBindAttribute_SimplePath_ShouldWork()
        {
            var attr = new UIBindAttribute("Button");
            Assert.AreEqual("Button", attr.Path);
        }

        /// <summary>
        /// 测试嵌套路径
        /// </summary>
        [Test]
        public void UIBindAttribute_NestedPath_ShouldWork()
        {
            var attr = new UIBindAttribute("Panel/SubPanel/Button");
            Assert.AreEqual("Panel/SubPanel/Button", attr.Path);
        }

        /// <summary>
        /// 测试深层嵌套路径
        /// </summary>
        [Test]
        public void UIBindAttribute_DeepNestedPath_ShouldWork()
        {
            var path = "Root/Level1/Level2/Level3/Level4/Target";
            var attr = new UIBindAttribute(path);
            Assert.AreEqual(path, attr.Path);
        }

        /// <summary>
        /// 测试包含特殊字符的路径
        /// </summary>
        [Test]
        public void UIBindAttribute_PathWithSpecialChars_ShouldWork()
        {
            var path = "Panel_Main/Button (1)";
            var attr = new UIBindAttribute(path);
            Assert.AreEqual(path, attr.Path);
        }

        #endregion

        #region 特性应用测试

        /// <summary>
        /// 测试特性可应用于字段
        /// </summary>
        [Test]
        public void UIBindAttribute_OnField_ShouldBeRetrievable()
        {
            var fieldInfo = typeof(TestClass).GetField("_button", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(fieldInfo, "字段应存在");
            
            var attr = System.Attribute.GetCustomAttribute(fieldInfo, typeof(UIBindAttribute)) as UIBindAttribute;
            Assert.IsNotNull(attr, "特性应存在");
            Assert.AreEqual("Panel/Button", attr.Path);
        }

        /// <summary>
        /// 测试特性可应用于公共字段
        /// </summary>
        [Test]
        public void UIBindAttribute_OnPublicField_ShouldBeRetrievable()
        {
            var fieldInfo = typeof(TestClass).GetField("PublicText", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(fieldInfo, "公共字段应存在");
            
            var attr = System.Attribute.GetCustomAttribute(fieldInfo, typeof(UIBindAttribute)) as UIBindAttribute;
            Assert.IsNotNull(attr, "特性应存在");
            Assert.AreEqual("Panel/Text", attr.Path);
        }

        /// <summary>
        /// 测试特性使用默认路径（字段名）
        /// </summary>
        [Test]
        public void UIBindAttribute_WithoutPath_ShouldUseFieldName()
        {
            var fieldInfo = typeof(TestClass).GetField("_image", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.IsNotNull(fieldInfo, "字段应存在");
            
            var attr = System.Attribute.GetCustomAttribute(fieldInfo, typeof(UIBindAttribute)) as UIBindAttribute;
            Assert.IsNotNull(attr, "特性应存在");
            Assert.IsNull(attr.Path, "未指定路径时Path应为null");
        }

        #endregion

        #region 测试辅助类

        /// <summary>
        /// 测试用类，包含带UIBind特性的字段
        /// </summary>
        private class TestClass
        {
            [UIBind("Panel/Button")]
            private UnityEngine.UI.Button _button;

            [UIBind("Panel/Text")]
            public TMPro.TextMeshProUGUI PublicText;

            [UIBind]
            private UnityEngine.UI.Image _image;
        }

        #endregion
    }
}
