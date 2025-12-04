using System;
using NUnit.Framework;
using xFrame.Runtime.UI;
using xFrame.Runtime.UI.Events;

namespace xFrame.Tests.EditMode.UITests
{
    /// <summary>
    /// UI事件结构体的单元测试
    /// </summary>
    [TestFixture]
    public class UIEventsTests
    {
        #region UIOpenedEvent测试

        /// <summary>
        /// 测试UIOpenedEvent构造函数
        /// </summary>
        [Test]
        public void UIOpenedEvent_Constructor_ShouldSetPropertiesCorrectly()
        {
            var uiType = typeof(TestUIView);
            var layer = UILayer.Normal;

            var evt = new UIOpenedEvent(uiType, null, layer);

            Assert.AreEqual(uiType, evt.UIType, "UIType应正确设置");
            Assert.IsNull(evt.View, "View应为null");
            Assert.AreEqual(layer, evt.Layer, "Layer应正确设置");
        }

        /// <summary>
        /// 测试UIOpenedEvent属性设置
        /// </summary>
        [Test]
        public void UIOpenedEvent_Properties_ShouldBeSettable()
        {
            var evt = new UIOpenedEvent();
            evt.UIType = typeof(TestUIView);
            evt.Layer = UILayer.Popup;

            Assert.AreEqual(typeof(TestUIView), evt.UIType);
            Assert.AreEqual(UILayer.Popup, evt.Layer);
        }

        #endregion

        #region UIClosedEvent测试

        /// <summary>
        /// 测试UIClosedEvent构造函数
        /// </summary>
        [Test]
        public void UIClosedEvent_Constructor_ShouldSetPropertiesCorrectly()
        {
            var uiType = typeof(TestUIView);
            var layer = UILayer.System;

            var evt = new UIClosedEvent(uiType, layer);

            Assert.AreEqual(uiType, evt.UIType, "UIType应正确设置");
            Assert.AreEqual(layer, evt.Layer, "Layer应正确设置");
        }

        /// <summary>
        /// 测试UIClosedEvent属性设置
        /// </summary>
        [Test]
        public void UIClosedEvent_Properties_ShouldBeSettable()
        {
            var evt = new UIClosedEvent();
            evt.UIType = typeof(TestUIView);
            evt.Layer = UILayer.Top;

            Assert.AreEqual(typeof(TestUIView), evt.UIType);
            Assert.AreEqual(UILayer.Top, evt.Layer);
        }

        #endregion

        #region UILayerChangedEvent测试

        /// <summary>
        /// 测试UILayerChangedEvent构造函数
        /// </summary>
        [Test]
        public void UILayerChangedEvent_Constructor_ShouldSetPropertiesCorrectly()
        {
            var layer = UILayer.Normal;
            var activeCount = 3;

            var evt = new UILayerChangedEvent(layer, activeCount);

            Assert.AreEqual(layer, evt.Layer, "Layer应正确设置");
            Assert.AreEqual(activeCount, evt.ActiveCount, "ActiveCount应正确设置");
        }

        /// <summary>
        /// 测试UILayerChangedEvent属性设置
        /// </summary>
        [Test]
        public void UILayerChangedEvent_Properties_ShouldBeSettable()
        {
            var evt = new UILayerChangedEvent();
            evt.Layer = UILayer.Popup;
            evt.ActiveCount = 5;

            Assert.AreEqual(UILayer.Popup, evt.Layer);
            Assert.AreEqual(5, evt.ActiveCount);
        }

        /// <summary>
        /// 测试UILayerChangedEvent零计数
        /// </summary>
        [Test]
        public void UILayerChangedEvent_ZeroCount_ShouldBeValid()
        {
            var evt = new UILayerChangedEvent(UILayer.Normal, 0);
            Assert.AreEqual(0, evt.ActiveCount, "ActiveCount可以为0");
        }

        #endregion

        #region UILoadStartEvent测试

        /// <summary>
        /// 测试UILoadStartEvent构造函数
        /// </summary>
        [Test]
        public void UILoadStartEvent_Constructor_ShouldSetPropertiesCorrectly()
        {
            var uiType = typeof(TestUIView);

            var evt = new UILoadStartEvent(uiType);

            Assert.AreEqual(uiType, evt.UIType, "UIType应正确设置");
        }

        /// <summary>
        /// 测试UILoadStartEvent属性设置
        /// </summary>
        [Test]
        public void UILoadStartEvent_Properties_ShouldBeSettable()
        {
            var evt = new UILoadStartEvent();
            evt.UIType = typeof(TestUIView);

            Assert.AreEqual(typeof(TestUIView), evt.UIType);
        }

        #endregion

        #region UILoadCompleteEvent测试

        /// <summary>
        /// 测试UILoadCompleteEvent成功场景
        /// </summary>
        [Test]
        public void UILoadCompleteEvent_Success_ShouldSetPropertiesCorrectly()
        {
            var uiType = typeof(TestUIView);
            var duration = 0.5f;

            var evt = new UILoadCompleteEvent(uiType, true, duration);

            Assert.AreEqual(uiType, evt.UIType, "UIType应正确设置");
            Assert.IsTrue(evt.Success, "Success应为true");
            Assert.AreEqual(duration, evt.Duration, 0.001f, "Duration应正确设置");
        }

        /// <summary>
        /// 测试UILoadCompleteEvent失败场景
        /// </summary>
        [Test]
        public void UILoadCompleteEvent_Failure_ShouldSetPropertiesCorrectly()
        {
            var uiType = typeof(TestUIView);
            var duration = 1.0f;

            var evt = new UILoadCompleteEvent(uiType, false, duration);

            Assert.AreEqual(uiType, evt.UIType, "UIType应正确设置");
            Assert.IsFalse(evt.Success, "Success应为false");
            Assert.AreEqual(duration, evt.Duration, 0.001f, "Duration应正确设置");
        }

        /// <summary>
        /// 测试UILoadCompleteEvent属性设置
        /// </summary>
        [Test]
        public void UILoadCompleteEvent_Properties_ShouldBeSettable()
        {
            var evt = new UILoadCompleteEvent();
            evt.UIType = typeof(TestUIView);
            evt.Success = true;
            evt.Duration = 2.5f;

            Assert.AreEqual(typeof(TestUIView), evt.UIType);
            Assert.IsTrue(evt.Success);
            Assert.AreEqual(2.5f, evt.Duration, 0.001f);
        }

        #endregion

        #region UINavigationEvent测试

        /// <summary>
        /// 测试UINavigationEvent前进导航
        /// </summary>
        [Test]
        public void UINavigationEvent_Forward_ShouldSetPropertiesCorrectly()
        {
            var fromType = typeof(TestUIView);
            var toType = typeof(TestUIView2);

            var evt = new UINavigationEvent(fromType, toType, NavigationType.Forward);

            Assert.AreEqual(fromType, evt.FromUIType, "FromUIType应正确设置");
            Assert.AreEqual(toType, evt.ToUIType, "ToUIType应正确设置");
            Assert.AreEqual(NavigationType.Forward, evt.Type, "Type应为Forward");
        }

        /// <summary>
        /// 测试UINavigationEvent返回导航
        /// </summary>
        [Test]
        public void UINavigationEvent_Back_ShouldSetPropertiesCorrectly()
        {
            var fromType = typeof(TestUIView2);
            var toType = typeof(TestUIView);

            var evt = new UINavigationEvent(fromType, toType, NavigationType.Back);

            Assert.AreEqual(fromType, evt.FromUIType, "FromUIType应正确设置");
            Assert.AreEqual(toType, evt.ToUIType, "ToUIType应正确设置");
            Assert.AreEqual(NavigationType.Back, evt.Type, "Type应为Back");
        }

        /// <summary>
        /// 测试UINavigationEvent从null开始的导航
        /// </summary>
        [Test]
        public void UINavigationEvent_FromNull_ShouldBeValid()
        {
            var evt = new UINavigationEvent(null, typeof(TestUIView), NavigationType.Forward);

            Assert.IsNull(evt.FromUIType, "FromUIType可以为null");
            Assert.AreEqual(typeof(TestUIView), evt.ToUIType);
        }

        #endregion

        #region NavigationType枚举测试

        /// <summary>
        /// 测试NavigationType枚举值
        /// </summary>
        [Test]
        public void NavigationType_EnumValues_ShouldBeCorrectlyDefined()
        {
            Assert.AreEqual(0, (int)NavigationType.Forward, "Forward应为0");
            Assert.AreEqual(1, (int)NavigationType.Back, "Back应为1");
        }

        /// <summary>
        /// 测试NavigationType枚举数量
        /// </summary>
        [Test]
        public void NavigationType_EnumCount_ShouldBeTwo()
        {
            var values = Enum.GetValues(typeof(NavigationType));
            Assert.AreEqual(2, values.Length, "NavigationType应有2个枚举值");
        }

        #endregion

        #region 测试辅助类

        /// <summary>
        /// 测试用UIView类型1
        /// </summary>
        private class TestUIView : UIView { }

        /// <summary>
        /// 测试用UIView类型2
        /// </summary>
        private class TestUIView2 : UIView { }

        #endregion
    }
}
