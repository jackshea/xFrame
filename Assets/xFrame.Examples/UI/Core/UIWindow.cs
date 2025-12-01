using UnityEngine;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI窗口基类
    /// 代表弹出式窗口或对话框，通常显示在其他UI之上
    /// </summary>
    public abstract class UIWindow : UIView
    {
        /// <summary>
        /// 是否为模态窗口
        /// 模态窗口会阻挡下层UI的交互
        /// </summary>
        public virtual bool IsModal => true;

        /// <summary>
        /// 点击遮罩是否关闭窗口
        /// </summary>
        public virtual bool CloseOnMaskClick => true;

        /// <summary>
        /// 遮罩透明度 (0-1)
        /// </summary>
        public virtual float MaskAlpha => 0.7f;

        /// <summary>
        /// 遮罩颜色
        /// </summary>
        public virtual Color MaskColor => Color.black;

        /// <summary>
        /// 窗口动画持续时间（秒）
        /// </summary>
        protected virtual float AnimationDuration => 0.2f;

        /// <summary>
        /// 遮罩GameObject（由UIManager创建）
        /// </summary>
        internal GameObject MaskObject { get; set; }

        /// <summary>
        /// 窗口层级，默认为Popup层
        /// </summary>
        public override UILayer Layer => UILayer.Popup;

        /// <summary>
        /// 窗口默认不使用导航栈
        /// </summary>
        public virtual bool UseStack => false;

        /// <summary>
        /// 窗口打开动画
        /// 默认实现缩放弹出效果
        /// </summary>
        protected virtual void PlayOpenAnimation()
        {
            if (RectTransform != null)
            {
                RectTransform.localScale = Vector3.zero;
                LeanTween.scale(RectTransform, Vector3.one, AnimationDuration)
                    .setEase(LeanTweenType.easeOutBack);
            }
        }

        /// <summary>
        /// 窗口关闭动画
        /// 默认实现缩放收缩效果
        /// </summary>
        protected virtual void PlayCloseAnimation()
        {
            if (RectTransform != null)
            {
                LeanTween.scale(RectTransform, Vector3.zero, AnimationDuration)
                    .setEase(LeanTweenType.easeInBack);
            }
        }

        /// <summary>
        /// 打开时播放动画
        /// </summary>
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            PlayOpenAnimation();
        }

        /// <summary>
        /// 关闭时播放动画
        /// </summary>
        protected override void OnClose()
        {
            PlayCloseAnimation();
            base.OnClose();
        }

        /// <summary>
        /// 处理遮罩点击事件
        /// </summary>
        internal void OnMaskClicked()
        {
            if (CloseOnMaskClick)
            {
                // 关闭窗口的逻辑由UIManager处理
            }
        }
    }
}

/* 
 * 注意：此示例代码使用了LeanTween作为动画库
 * 如果项目中没有LeanTween，可以：
 * 1. 安装LeanTween插件
 * 2. 使用其他动画库（如DOTween）替换
 * 3. 使用Unity的Animator或自定义动画实现
 * 
 * 示例替换为DOTween：
 * RectTransform.DOScale(Vector3.one, AnimationDuration).SetEase(Ease.OutBack);
 */
