using UnityEngine;
using VContainer;
using xFrame.Runtime.UI;
using System.Threading.Tasks;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// UI导航栈与生命周期示例
    /// 展示OnShow和OnHide在导航栈中的使用
    /// </summary>
    public class UINavigationStackExample : MonoBehaviour
    {
        private IUIManager _uiManager;

        [Inject]
        public void Construct(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        private async void Start()
        {
            Debug.Log("=== UI导航栈生命周期示例 ===\n");

            // 演示完整的导航栈生命周期
            await DemonstrateNavigationLifecycle();
        }

        /// <summary>
        /// 演示导航栈生命周期
        /// </summary>
        private async Task DemonstrateNavigationLifecycle()
        {
            Debug.Log("【场景1】打开UI A");
            Debug.Log("预期生命周期: OnCreate → OnOpen → OnShow\n");
            // await _uiManager.OpenAsync<UIPanelA>();
            await Task.Delay(1000);

            Debug.Log("【场景2】打开UI B（A被压入栈）");
            Debug.Log("预期生命周期:");
            Debug.Log("  - A: OnHide （被遮挡）");
            Debug.Log("  - B: OnCreate → OnOpen → OnShow\n");
            // await _uiManager.OpenAsync<UIPanelB>();
            await Task.Delay(1000);

            Debug.Log("【场景3】打开UI C（B被压入栈）");
            Debug.Log("预期生命周期:");
            Debug.Log("  - B: OnHide （被遮挡）");
            Debug.Log("  - C: OnCreate → OnOpen → OnShow\n");
            // await _uiManager.OpenAsync<UIPanelC>();
            await Task.Delay(1000);

            Debug.Log("【场景4】Back返回到B（C关闭，B恢复）");
            Debug.Log("预期生命周期:");
            Debug.Log("  - C: OnHide → OnClose");
            Debug.Log("  - B: OnShow （从栈中恢复）\n");
            // _uiManager.Back();
            await Task.Delay(1000);

            Debug.Log("【场景5】Back返回到A（B关闭，A恢复）");
            Debug.Log("预期生命周期:");
            Debug.Log("  - B: OnHide → OnClose");
            Debug.Log("  - A: OnShow （从栈中恢复）\n");
            // _uiManager.Back();
            await Task.Delay(1000);

            Debug.Log("【场景6】关闭A");
            Debug.Log("预期生命周期: OnHide → OnClose → OnDestroy\n");
            // _uiManager.Close<UIPanelA>();

            Debug.Log("=== 示例完成 ===");
        }
    }

    #region 示例UI面板 - 展示生命周期

    /// <summary>
    /// 示例UI面板A
    /// 展示完整的生命周期调用
    /// </summary>
    public class UIPanelA : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;

        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("  [UIPanelA] OnCreate - 面板创建");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            Debug.Log("  [UIPanelA] OnOpen - 面板打开");
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("  [UIPanelA] OnShow - 面板显示");
            
            // ✅ 在这里执行显示逻辑
            // - 播放显示动画
            // - 恢复背景音乐
            // - 开始更新循环
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("  [UIPanelA] OnHide - 面板隐藏");
            
            // ✅ 在这里执行隐藏逻辑
            // - 暂停动画
            // - 降低音量
            // - 停止更新循环
        }

        protected override void OnClose()
        {
            base.OnClose();
            Debug.Log("  [UIPanelA] OnClose - 面板关闭");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("  [UIPanelA] OnDestroy - 面板销毁");
        }
    }

    /// <summary>
    /// 示例UI面板B
    /// </summary>
    public class UIPanelB : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;

        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("  [UIPanelB] OnCreate - 面板创建");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            Debug.Log("  [UIPanelB] OnOpen - 面板打开");
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("  [UIPanelB] OnShow - 面板显示");
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("  [UIPanelB] OnHide - 面板隐藏");
        }

        protected override void OnClose()
        {
            base.OnClose();
            Debug.Log("  [UIPanelB] OnClose - 面板关闭");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("  [UIPanelB] OnDestroy - 面板销毁");
        }
    }

    /// <summary>
    /// 示例UI面板C
    /// </summary>
    public class UIPanelC : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;

        protected override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("  [UIPanelC] OnCreate - 面板创建");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            Debug.Log("  [UIPanelC] OnOpen - 面板打开");
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("  [UIPanelC] OnShow - 面板显示");
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("  [UIPanelC] OnHide - 面板隐藏");
        }

        protected override void OnClose()
        {
            base.OnClose();
            Debug.Log("  [UIPanelC] OnClose - 面板关闭");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("  [UIPanelC] OnDestroy - 面板销毁");
        }
    }

    #endregion

    #region 实际应用示例 - 带动画和音效的UI

    /// <summary>
    /// 实际应用示例：带动画和音效的UI面板
    /// 展示如何在OnShow/OnHide中处理实际业务逻辑
    /// </summary>
    public class GameMenuPanel : UIPanel
    {
        private bool _isAnimationPlaying;
        private bool _isUpdateActive;

        protected override void OnShow()
        {
            base.OnShow();

            // 1. 播放显示动画
            PlayShowAnimation();

            // 2. 恢复或播放背景音乐
            // AudioManager.Instance.PlayBGM("MenuMusic");
            // AudioManager.Instance.SetMusicVolume(1.0f);

            // 3. 开始更新逻辑（如果需要）
            _isUpdateActive = true;

            // 4. 刷新UI数据（可能在被遮挡期间数据已更新）
            RefreshUIData();

            Debug.Log("[GameMenuPanel] 面板显示，已恢复所有功能");
        }

        protected override void OnHide()
        {
            base.OnHide();

            // 1. 播放隐藏动画
            PlayHideAnimation();

            // 2. 降低或暂停背景音乐
            // AudioManager.Instance.SetMusicVolume(0.3f);

            // 3. 停止更新逻辑，节省性能
            _isUpdateActive = false;

            // 4. 保存临时状态（如果需要）
            // SaveTemporaryState();

            Debug.Log("[GameMenuPanel] 面板隐藏，已暂停大部分功能");
        }

        private void PlayShowAnimation()
        {
            if (_isAnimationPlaying) return;
            
            _isAnimationPlaying = true;
            
            // 使用DOTween或LeanTween播放动画
            // transform.DOScale(Vector3.one, 0.3f).OnComplete(() => _isAnimationPlaying = false);
            
            _isAnimationPlaying = false;
        }

        private void PlayHideAnimation()
        {
            // 播放隐藏动画（可选）
        }

        private void RefreshUIData()
        {
            // 刷新UI显示的数据
            Debug.Log("[GameMenuPanel] 刷新UI数据");
        }

        private void Update()
        {
            // 只在激活状态下更新
            if (!_isUpdateActive) return;

            // 执行需要每帧更新的逻辑
        }
    }

    #endregion
}
