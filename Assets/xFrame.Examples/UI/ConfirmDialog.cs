using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using xFrame.Runtime.UI;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// 确认对话框示例
    /// 展示如何创建一个弹窗式对话框
    /// </summary>
    public class ConfirmDialog : UIWindow
    {
        #region UI组件

        [Header("文本组件")]
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI messageText;

        [Header("按钮组件")]
        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private TextMeshProUGUI confirmButtonText;

        [SerializeField]
        private TextMeshProUGUI cancelButtonText;

        #endregion

        #region 依赖注入

        private IUIManager _uiManager;

        [Inject]
        public void Construct(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        #endregion

        #region UI配置

        /// <summary>
        /// 设置为弹窗层
        /// </summary>
        public override UILayer Layer => UILayer.Popup;

        /// <summary>
        /// 模态窗口，阻挡下层交互
        /// </summary>
        public override bool IsModal => true;

        /// <summary>
        /// 点击遮罩不关闭（需要用户明确选择）
        /// </summary>
        public override bool CloseOnMaskClick => false;

        /// <summary>
        /// 遮罩透明度
        /// </summary>
        public override float MaskAlpha => 0.8f;

        /// <summary>
        /// 对话框不使用导航栈
        /// </summary>
        public override bool UseStack => false;

        /// <summary>
        /// 允许缓存
        /// </summary>
        public override bool Cacheable => true;

        #endregion

        #region 私有字段

        private Action _onConfirm;
        private Action _onCancel;

        #endregion

        #region 生命周期回调

        /// <summary>
        /// UI创建时调用
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // 绑定按钮事件
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            Debug.Log("[ConfirmDialog] UI创建完成");
        }

        /// <summary>
        /// UI打开时调用
        /// </summary>
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            // 解析数据
            if (data is ConfirmDialogData dialogData)
            {
                // 设置文本
                if (titleText != null)
                    titleText.text = dialogData.Title ?? "提示";

                if (messageText != null)
                    messageText.text = dialogData.Message ?? "";

                // 设置按钮文本
                if (confirmButtonText != null)
                    confirmButtonText.text = dialogData.ConfirmText ?? "确定";

                if (cancelButtonText != null)
                    cancelButtonText.text = dialogData.CancelText ?? "取消";

                // 保存回调
                _onConfirm = dialogData.OnConfirm;
                _onCancel = dialogData.OnCancel;

                // 控制取消按钮显示
                if (cancelButton != null)
                    cancelButton.gameObject.SetActive(dialogData.ShowCancelButton);
            }

            Debug.Log($"[ConfirmDialog] 对话框已打开: {messageText?.text}");
        }

        /// <summary>
        /// UI关闭时调用
        /// </summary>
        protected override void OnClose()
        {
            base.OnClose();

            // 清空回调
            _onConfirm = null;
            _onCancel = null;

            Debug.Log("[ConfirmDialog] 对话框已关闭");
        }

        /// <summary>
        /// UI销毁时调用
        /// </summary>
        protected override void OnUIDestroy()
        {
            // 取消按钮事件
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelButtonClicked);

            base.OnUIDestroy();
            Debug.Log("[ConfirmDialog] UI销毁完成");
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 确认按钮点击
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            Debug.Log("[ConfirmDialog] 点击确认");

            // 执行确认回调
            _onConfirm?.Invoke();

            // 关闭对话框
            _uiManager.Close(this);
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void OnCancelButtonClicked()
        {
            Debug.Log("[ConfirmDialog] 点击取消");

            // 执行取消回调
            _onCancel?.Invoke();

            // 关闭对话框
            _uiManager.Close(this);
        }

        #endregion

        #region 动画重写（可选）

        /// <summary>
        /// 自定义打开动画
        /// </summary>
        protected override void PlayOpenAnimation()
        {
            // 可以使用LeanTween、DOTween或Unity Animator
            // 这里展示一个简单的缩放弹出效果

            if (RectTransform != null)
            {
                RectTransform.localScale = Vector3.zero;

                // 使用LeanTween（需要安装LeanTween插件）
                // LeanTween.scale(RectTransform, Vector3.one, AnimationDuration)
                //     .setEase(LeanTweenType.easeOutBack);

                // 或者使用简单的插值
                StartCoroutine(ScaleAnimation(Vector3.zero, Vector3.one, AnimationDuration));
            }
        }

        /// <summary>
        /// 自定义关闭动画
        /// </summary>
        protected override void PlayCloseAnimation()
        {
            if (RectTransform != null)
                // LeanTween.scale(RectTransform, Vector3.zero, AnimationDuration)
                //     .setEase(LeanTweenType.easeInBack);
                StartCoroutine(ScaleAnimation(Vector3.one, Vector3.zero, AnimationDuration));
        }

        /// <summary>
        /// 简单的缩放动画协程
        /// </summary>
        private IEnumerator ScaleAnimation(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;

                // 使用EaseOutBack曲线
                t = EaseOutBack(t);

                RectTransform.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }

            RectTransform.localScale = to;
        }

        /// <summary>
        /// EaseOutBack缓动函数
        /// </summary>
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #endregion
    }

    #region 数据类

    /// <summary>
    /// 确认对话框数据
    /// </summary>
    public class ConfirmDialogData
    {
        /// <summary>
        /// 标题文本
        /// </summary>
        public string Title { get; set; } = "提示";

        /// <summary>
        /// 消息文本
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        public string ConfirmText { get; set; } = "确定";

        /// <summary>
        /// 取消按钮文本
        /// </summary>
        public string CancelText { get; set; } = "取消";

        /// <summary>
        /// 是否显示取消按钮
        /// </summary>
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>
        /// 确认回调
        /// </summary>
        public Action OnConfirm { get; set; }

        /// <summary>
        /// 取消回调
        /// </summary>
        public Action OnCancel { get; set; }

        /// <summary>
        /// 创建一个简单的提示对话框（只有确定按钮）
        /// </summary>
        public static ConfirmDialogData CreateAlert(string message, string title = "提示", Action onConfirm = null)
        {
            return new ConfirmDialogData
            {
                Title = title,
                Message = message,
                ShowCancelButton = false,
                OnConfirm = onConfirm
            };
        }

        /// <summary>
        /// 创建一个确认对话框（有确定和取消按钮）
        /// </summary>
        public static ConfirmDialogData CreateConfirm(string message, string title = "确认",
            Action onConfirm = null, Action onCancel = null)
        {
            return new ConfirmDialogData
            {
                Title = title,
                Message = message,
                ShowCancelButton = true,
                OnConfirm = onConfirm,
                OnCancel = onCancel
            };
        }
    }

    #endregion
}