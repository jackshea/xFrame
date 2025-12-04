using System;
using System.Threading.Tasks;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
#endif

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI动画接口
    /// 定义UI动画的标准行为
    /// </summary>
    public interface IUIAnimation
    {
        /// <summary>
        /// 动画持续时间（秒）
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="target">动画目标</param>
        /// <param name="onComplete">完成回调</param>
        void Play(RectTransform target, Action onComplete = null);

        /// <summary>
        /// 异步播放动画
        /// </summary>
        /// <param name="target">动画目标</param>
        /// <returns>动画任务</returns>
        Task PlayAsync(RectTransform target);

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="target">动画目标</param>
        void Stop(RectTransform target);
    }

    /// <summary>
    /// UI动画基类
    /// 提供动画的基础实现
    /// </summary>
    public abstract class UIAnimationBase : IUIAnimation
    {
        /// <summary>
        /// 动画持续时间（秒）
        /// </summary>
        public virtual float Duration => 0.3f;

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="target">动画目标</param>
        /// <param name="onComplete">完成回调</param>
        public abstract void Play(RectTransform target, Action onComplete = null);

        /// <summary>
        /// 异步播放动画
        /// </summary>
        /// <param name="target">动画目标</param>
        /// <returns>动画任务</returns>
        public virtual async Task PlayAsync(RectTransform target)
        {
            var tcs = new TaskCompletionSource<bool>();
            Play(target, () => tcs.SetResult(true));
            await tcs.Task;
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="target">动画目标</param>
        public abstract void Stop(RectTransform target);
    }

    /// <summary>
    /// 缩放弹出动画
    /// 从小到大的弹出效果
    /// </summary>
    public class ScalePopAnimation : UIAnimationBase
    {
        private readonly float _duration;
        private readonly Vector3 _startScale;
        private readonly Vector3 _endScale;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="duration">动画持续时间</param>
        /// <param name="startScale">起始缩放</param>
        /// <param name="endScale">结束缩放</param>
        public ScalePopAnimation(float duration = 0.3f, Vector3? startScale = null, Vector3? endScale = null)
        {
            _duration = duration;
            _startScale = startScale ?? Vector3.zero;
            _endScale = endScale ?? Vector3.one;
        }

        public override float Duration => _duration;

        public override void Play(RectTransform target, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            target.localScale = _startScale;

#if DOTWEEN
            // 使用DOTween实现
            target.DOScale(_endScale, _duration)
                .SetEase(DG.Tweening.Ease.OutBack)
                .OnComplete(() => onComplete?.Invoke());
#else
            // 使用协程实现（需要MonoBehaviour）
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner == null)
                coroutineRunner = target.gameObject.AddComponent<UIAnimationCoroutineRunner>();

            coroutineRunner.StartScaleAnimation(target, _startScale, _endScale, _duration, onComplete);
#endif
        }

        public override void Stop(RectTransform target)
        {
            if (target == null) return;

#if DOTWEEN
            target.DOKill();
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner != null)
                coroutineRunner.StopAllCoroutines();
#endif
        }
    }

    /// <summary>
    /// 缩放收缩动画
    /// 从大到小的收缩效果
    /// </summary>
    public class ScaleShrinkAnimation : UIAnimationBase
    {
        private readonly float _duration;
        private readonly Vector3 _startScale;
        private readonly Vector3 _endScale;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="duration">动画持续时间</param>
        /// <param name="startScale">起始缩放</param>
        /// <param name="endScale">结束缩放</param>
        public ScaleShrinkAnimation(float duration = 0.2f, Vector3? startScale = null, Vector3? endScale = null)
        {
            _duration = duration;
            _startScale = startScale ?? Vector3.one;
            _endScale = endScale ?? Vector3.zero;
        }

        public override float Duration => _duration;

        public override void Play(RectTransform target, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            target.localScale = _startScale;

#if DOTWEEN
            target.DOScale(_endScale, _duration)
                .SetEase(DG.Tweening.Ease.InBack)
                .OnComplete(() => onComplete?.Invoke());
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner == null)
                coroutineRunner = target.gameObject.AddComponent<UIAnimationCoroutineRunner>();

            coroutineRunner.StartScaleAnimation(target, _startScale, _endScale, _duration, onComplete);
#endif
        }

        public override void Stop(RectTransform target)
        {
            if (target == null) return;

#if DOTWEEN
            target.DOKill();
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner != null)
                coroutineRunner.StopAllCoroutines();
#endif
        }
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    public class FadeInAnimation : UIAnimationBase
    {
        private readonly float _duration;

        public FadeInAnimation(float duration = 0.3f)
        {
            _duration = duration;
        }

        public override float Duration => _duration;

        public override void Play(RectTransform target, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;

#if DOTWEEN
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, _duration)
                .OnComplete(() => onComplete?.Invoke());
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner == null)
                coroutineRunner = target.gameObject.AddComponent<UIAnimationCoroutineRunner>();

            coroutineRunner.StartFadeAnimation(canvasGroup, 0f, 1f, _duration, onComplete);
#endif
        }

        public override void Stop(RectTransform target)
        {
            if (target == null) return;

#if DOTWEEN
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.DOKill();
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner != null)
                coroutineRunner.StopAllCoroutines();
#endif
        }
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    public class FadeOutAnimation : UIAnimationBase
    {
        private readonly float _duration;

        public FadeOutAnimation(float duration = 0.2f)
        {
            _duration = duration;
        }

        public override float Duration => _duration;

        public override void Play(RectTransform target, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;

#if DOTWEEN
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, _duration)
                .OnComplete(() => onComplete?.Invoke());
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner == null)
                coroutineRunner = target.gameObject.AddComponent<UIAnimationCoroutineRunner>();

            coroutineRunner.StartFadeAnimation(canvasGroup, 1f, 0f, _duration, onComplete);
#endif
        }

        public override void Stop(RectTransform target)
        {
            if (target == null) return;

#if DOTWEEN
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.DOKill();
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner != null)
                coroutineRunner.StopAllCoroutines();
#endif
        }
    }

    /// <summary>
    /// 滑入动画
    /// </summary>
    public class SlideInAnimation : UIAnimationBase
    {
        private readonly float _duration;
        private readonly SlideDirection _direction;

        public enum SlideDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }

        public SlideInAnimation(SlideDirection direction = SlideDirection.Bottom, float duration = 0.3f)
        {
            _direction = direction;
            _duration = duration;
        }

        public override float Duration => _duration;

        public override void Play(RectTransform target, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            var endPos = target.anchoredPosition;
            var startPos = GetStartPosition(target, _direction);
            target.anchoredPosition = startPos;

#if DOTWEEN
            DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, endPos, _duration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => onComplete?.Invoke());
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner == null)
                coroutineRunner = target.gameObject.AddComponent<UIAnimationCoroutineRunner>();

            coroutineRunner.StartMoveAnimation(target, startPos, endPos, _duration, onComplete);
#endif
        }

        private Vector2 GetStartPosition(RectTransform target, SlideDirection direction)
        {
            var rect = target.rect;
            var currentPos = target.anchoredPosition;

            return direction switch
            {
                SlideDirection.Left => new Vector2(currentPos.x - rect.width, currentPos.y),
                SlideDirection.Right => new Vector2(currentPos.x + rect.width, currentPos.y),
                SlideDirection.Top => new Vector2(currentPos.x, currentPos.y + rect.height),
                SlideDirection.Bottom => new Vector2(currentPos.x, currentPos.y - rect.height),
                _ => currentPos
            };
        }

        public override void Stop(RectTransform target)
        {
            if (target == null) return;

#if DOTWEEN
            target.DOKill();
#else
            var coroutineRunner = target.GetComponent<UIAnimationCoroutineRunner>();
            if (coroutineRunner != null)
                coroutineRunner.StopAllCoroutines();
#endif
        }
    }

    /// <summary>
    /// UI动画协程运行器
    /// 用于在没有DOTween时运行动画
    /// </summary>
    public class UIAnimationCoroutineRunner : MonoBehaviour
    {
        /// <summary>
        /// 开始缩放动画
        /// </summary>
        public void StartScaleAnimation(RectTransform target, Vector3 from, Vector3 to, float duration, Action onComplete)
        {
            StartCoroutine(ScaleCoroutine(target, from, to, duration, onComplete));
        }

        /// <summary>
        /// 开始淡入淡出动画
        /// </summary>
        public void StartFadeAnimation(CanvasGroup canvasGroup, float from, float to, float duration, Action onComplete)
        {
            StartCoroutine(FadeCoroutine(canvasGroup, from, to, duration, onComplete));
        }

        /// <summary>
        /// 开始移动动画
        /// </summary>
        public void StartMoveAnimation(RectTransform target, Vector2 from, Vector2 to, float duration, Action onComplete)
        {
            StartCoroutine(MoveCoroutine(target, from, to, duration, onComplete));
        }

        private System.Collections.IEnumerator ScaleCoroutine(RectTransform target, Vector3 from, Vector3 to, float duration, Action onComplete)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = EaseOutBack(elapsed / duration);
                target.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            target.localScale = to;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float from, float to, float duration, Action onComplete)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator MoveCoroutine(RectTransform target, Vector2 from, Vector2 to, float duration, Action onComplete)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = EaseOutCubic(elapsed / duration);
                target.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            target.anchoredPosition = to;
            onComplete?.Invoke();
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

        /// <summary>
        /// EaseOutCubic缓动函数
        /// </summary>
        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }

    /// <summary>
    /// 预定义的UI动画集合
    /// </summary>
    public static class UIAnimations
    {
        /// <summary>
        /// 默认弹出动画
        /// </summary>
        public static IUIAnimation PopIn => new ScalePopAnimation();

        /// <summary>
        /// 默认收缩动画
        /// </summary>
        public static IUIAnimation PopOut => new ScaleShrinkAnimation();

        /// <summary>
        /// 默认淡入动画
        /// </summary>
        public static IUIAnimation FadeIn => new FadeInAnimation();

        /// <summary>
        /// 默认淡出动画
        /// </summary>
        public static IUIAnimation FadeOut => new FadeOutAnimation();

        /// <summary>
        /// 从底部滑入
        /// </summary>
        public static IUIAnimation SlideInFromBottom => new SlideInAnimation(SlideInAnimation.SlideDirection.Bottom);

        /// <summary>
        /// 从顶部滑入
        /// </summary>
        public static IUIAnimation SlideInFromTop => new SlideInAnimation(SlideInAnimation.SlideDirection.Top);

        /// <summary>
        /// 从左侧滑入
        /// </summary>
        public static IUIAnimation SlideInFromLeft => new SlideInAnimation(SlideInAnimation.SlideDirection.Left);

        /// <summary>
        /// 从右侧滑入
        /// </summary>
        public static IUIAnimation SlideInFromRight => new SlideInAnimation(SlideInAnimation.SlideDirection.Right);
    }
}
