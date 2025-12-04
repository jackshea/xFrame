using NUnit.Framework;
using xFrame.Runtime.UI;

namespace xFrame.Tests.EditMode.UITests
{
    /// <summary>
    /// UILayer枚举和扩展方法的单元测试
    /// </summary>
    [TestFixture]
    public class UILayerTests
    {
        #region UILayer枚举值测试

        /// <summary>
        /// 测试UILayer枚举值是否正确定义
        /// </summary>
        [Test]
        public void UILayer_EnumValues_ShouldBeCorrectlyDefined()
        {
            Assert.AreEqual(0, (int)UILayer.Background, "Background层应为0");
            Assert.AreEqual(1, (int)UILayer.Normal, "Normal层应为1");
            Assert.AreEqual(2, (int)UILayer.Popup, "Popup层应为2");
            Assert.AreEqual(3, (int)UILayer.System, "System层应为3");
            Assert.AreEqual(4, (int)UILayer.Top, "Top层应为4");
        }

        /// <summary>
        /// 测试UILayer枚举值数量
        /// </summary>
        [Test]
        public void UILayer_EnumCount_ShouldBeFive()
        {
            var values = System.Enum.GetValues(typeof(UILayer));
            Assert.AreEqual(5, values.Length, "UILayer应有5个枚举值");
        }

        #endregion

        #region GetBaseSortOrder扩展方法测试

        /// <summary>
        /// 测试Background层的基础SortOrder
        /// </summary>
        [Test]
        public void GetBaseSortOrder_Background_ShouldReturnZero()
        {
            var sortOrder = UILayer.Background.GetBaseSortOrder();
            Assert.AreEqual(0, sortOrder, "Background层的SortOrder应为0");
        }

        /// <summary>
        /// 测试Normal层的基础SortOrder
        /// </summary>
        [Test]
        public void GetBaseSortOrder_Normal_ShouldReturn1000()
        {
            var sortOrder = UILayer.Normal.GetBaseSortOrder();
            Assert.AreEqual(1000, sortOrder, "Normal层的SortOrder应为1000");
        }

        /// <summary>
        /// 测试Popup层的基础SortOrder
        /// </summary>
        [Test]
        public void GetBaseSortOrder_Popup_ShouldReturn2000()
        {
            var sortOrder = UILayer.Popup.GetBaseSortOrder();
            Assert.AreEqual(2000, sortOrder, "Popup层的SortOrder应为2000");
        }

        /// <summary>
        /// 测试System层的基础SortOrder
        /// </summary>
        [Test]
        public void GetBaseSortOrder_System_ShouldReturn3000()
        {
            var sortOrder = UILayer.System.GetBaseSortOrder();
            Assert.AreEqual(3000, sortOrder, "System层的SortOrder应为3000");
        }

        /// <summary>
        /// 测试Top层的基础SortOrder
        /// </summary>
        [Test]
        public void GetBaseSortOrder_Top_ShouldReturn4000()
        {
            var sortOrder = UILayer.Top.GetBaseSortOrder();
            Assert.AreEqual(4000, sortOrder, "Top层的SortOrder应为4000");
        }

        /// <summary>
        /// 测试所有层级的SortOrder递增关系
        /// </summary>
        [Test]
        public void GetBaseSortOrder_AllLayers_ShouldBeInAscendingOrder()
        {
            var backgroundSort = UILayer.Background.GetBaseSortOrder();
            var normalSort = UILayer.Normal.GetBaseSortOrder();
            var popupSort = UILayer.Popup.GetBaseSortOrder();
            var systemSort = UILayer.System.GetBaseSortOrder();
            var topSort = UILayer.Top.GetBaseSortOrder();

            Assert.Less(backgroundSort, normalSort, "Background应小于Normal");
            Assert.Less(normalSort, popupSort, "Normal应小于Popup");
            Assert.Less(popupSort, systemSort, "Popup应小于System");
            Assert.Less(systemSort, topSort, "System应小于Top");
        }

        /// <summary>
        /// 测试层级间隔是否为1000
        /// </summary>
        [Test]
        public void GetBaseSortOrder_LayerGap_ShouldBe1000()
        {
            var normalSort = UILayer.Normal.GetBaseSortOrder();
            var popupSort = UILayer.Popup.GetBaseSortOrder();
            var gap = popupSort - normalSort;

            Assert.AreEqual(1000, gap, "层级间隔应为1000");
        }

        #endregion

        #region GetCanvasName扩展方法测试

        /// <summary>
        /// 测试Background层的Canvas名称
        /// </summary>
        [Test]
        public void GetCanvasName_Background_ShouldReturnCorrectName()
        {
            var name = UILayer.Background.GetCanvasName();
            Assert.AreEqual("Canvas_Background", name);
        }

        /// <summary>
        /// 测试Normal层的Canvas名称
        /// </summary>
        [Test]
        public void GetCanvasName_Normal_ShouldReturnCorrectName()
        {
            var name = UILayer.Normal.GetCanvasName();
            Assert.AreEqual("Canvas_Normal", name);
        }

        /// <summary>
        /// 测试Popup层的Canvas名称
        /// </summary>
        [Test]
        public void GetCanvasName_Popup_ShouldReturnCorrectName()
        {
            var name = UILayer.Popup.GetCanvasName();
            Assert.AreEqual("Canvas_Popup", name);
        }

        /// <summary>
        /// 测试System层的Canvas名称
        /// </summary>
        [Test]
        public void GetCanvasName_System_ShouldReturnCorrectName()
        {
            var name = UILayer.System.GetCanvasName();
            Assert.AreEqual("Canvas_System", name);
        }

        /// <summary>
        /// 测试Top层的Canvas名称
        /// </summary>
        [Test]
        public void GetCanvasName_Top_ShouldReturnCorrectName()
        {
            var name = UILayer.Top.GetCanvasName();
            Assert.AreEqual("Canvas_Top", name);
        }

        /// <summary>
        /// 测试所有层级的Canvas名称格式
        /// </summary>
        [Test]
        public void GetCanvasName_AllLayers_ShouldFollowNamingConvention()
        {
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                var name = layer.GetCanvasName();
                Assert.IsTrue(name.StartsWith("Canvas_"), $"Canvas名称应以'Canvas_'开头: {name}");
                Assert.IsTrue(name.Contains(layer.ToString()), $"Canvas名称应包含层级名称: {name}");
            }
        }

        #endregion
    }
}
