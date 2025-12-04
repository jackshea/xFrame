using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI绑定辅助工具
    /// 简化UI组件引用的获取和管理
    /// </summary>
    public static class UIBinder
    {
        #region 查找组件

        /// <summary>
        /// 通过路径查找子对象
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">路径（如 "Panel/Button"）</param>
        /// <returns>找到的Transform</returns>
        public static Transform FindChild(this Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            return root.Find(path);
        }

        /// <summary>
        /// 通过路径查找组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="root">根节点</param>
        /// <param name="path">路径（如 "Panel/Button"）</param>
        /// <returns>找到的组件</returns>
        public static T FindComponent<T>(this Transform root, string path) where T : Component
        {
            var child = root.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        /// <summary>
        /// 通过名称递归查找子对象
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="name">对象名称</param>
        /// <returns>找到的Transform</returns>
        public static Transform FindChildRecursive(this Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            // 先检查直接子对象
            var child = root.Find(name);
            if (child != null)
                return child;

            // 递归查找
            foreach (Transform t in root)
            {
                var result = t.FindChildRecursive(name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 通过名称递归查找组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="root">根节点</param>
        /// <param name="name">对象名称</param>
        /// <returns>找到的组件</returns>
        public static T FindComponentRecursive<T>(this Transform root, string name) where T : Component
        {
            var child = root.FindChildRecursive(name);
            return child != null ? child.GetComponent<T>() : null;
        }

        #endregion

        #region 快捷绑定

        /// <summary>
        /// 绑定Button点击事件
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">按钮路径</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>Button组件</returns>
        public static Button BindButton(this Transform root, string path, Action onClick)
        {
            var button = root.FindComponent<Button>(path);
            if (button != null && onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            return button;
        }

        /// <summary>
        /// 绑定Toggle值改变事件
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Toggle路径</param>
        /// <param name="onValueChanged">值改变回调</param>
        /// <returns>Toggle组件</returns>
        public static Toggle BindToggle(this Transform root, string path, Action<bool> onValueChanged)
        {
            var toggle = root.FindComponent<Toggle>(path);
            if (toggle != null && onValueChanged != null)
            {
                toggle.onValueChanged.AddListener(value => onValueChanged(value));
            }
            return toggle;
        }

        /// <summary>
        /// 绑定Slider值改变事件
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Slider路径</param>
        /// <param name="onValueChanged">值改变回调</param>
        /// <returns>Slider组件</returns>
        public static Slider BindSlider(this Transform root, string path, Action<float> onValueChanged)
        {
            var slider = root.FindComponent<Slider>(path);
            if (slider != null && onValueChanged != null)
            {
                slider.onValueChanged.AddListener(value => onValueChanged(value));
            }
            return slider;
        }

        /// <summary>
        /// 绑定InputField值改变事件
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">InputField路径</param>
        /// <param name="onValueChanged">值改变回调</param>
        /// <param name="onEndEdit">结束编辑回调</param>
        /// <returns>TMP_InputField组件</returns>
        public static TMP_InputField BindInputField(this Transform root, string path, 
            Action<string> onValueChanged = null, Action<string> onEndEdit = null)
        {
            var inputField = root.FindComponent<TMP_InputField>(path);
            if (inputField != null)
            {
                if (onValueChanged != null)
                    inputField.onValueChanged.AddListener(value => onValueChanged(value));
                if (onEndEdit != null)
                    inputField.onEndEdit.AddListener(value => onEndEdit(value));
            }
            return inputField;
        }

        /// <summary>
        /// 绑定Dropdown值改变事件
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Dropdown路径</param>
        /// <param name="onValueChanged">值改变回调</param>
        /// <returns>TMP_Dropdown组件</returns>
        public static TMP_Dropdown BindDropdown(this Transform root, string path, Action<int> onValueChanged)
        {
            var dropdown = root.FindComponent<TMP_Dropdown>(path);
            if (dropdown != null && onValueChanged != null)
            {
                dropdown.onValueChanged.AddListener(value => onValueChanged(value));
            }
            return dropdown;
        }

        #endregion

        #region 文本设置

        /// <summary>
        /// 设置Text文本
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Text路径</param>
        /// <param name="text">文本内容</param>
        /// <returns>TextMeshProUGUI组件</returns>
        public static TextMeshProUGUI SetText(this Transform root, string path, string text)
        {
            var textComponent = root.FindComponent<TextMeshProUGUI>(path);
            if (textComponent != null)
            {
                textComponent.text = text;
            }
            return textComponent;
        }

        /// <summary>
        /// 获取Text文本
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Text路径</param>
        /// <returns>文本内容</returns>
        public static string GetText(this Transform root, string path)
        {
            var textComponent = root.FindComponent<TextMeshProUGUI>(path);
            return textComponent != null ? textComponent.text : string.Empty;
        }

        #endregion

        #region 图片设置

        /// <summary>
        /// 设置Image图片
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Image路径</param>
        /// <param name="sprite">图片</param>
        /// <returns>Image组件</returns>
        public static Image SetSprite(this Transform root, string path, Sprite sprite)
        {
            var image = root.FindComponent<Image>(path);
            if (image != null)
            {
                image.sprite = sprite;
            }
            return image;
        }

        /// <summary>
        /// 设置Image颜色
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Image路径</param>
        /// <param name="color">颜色</param>
        /// <returns>Image组件</returns>
        public static Image SetColor(this Transform root, string path, Color color)
        {
            var image = root.FindComponent<Image>(path);
            if (image != null)
            {
                image.color = color;
            }
            return image;
        }

        /// <summary>
        /// 设置Image填充量
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">Image路径</param>
        /// <param name="fillAmount">填充量（0-1）</param>
        /// <returns>Image组件</returns>
        public static Image SetFillAmount(this Transform root, string path, float fillAmount)
        {
            var image = root.FindComponent<Image>(path);
            if (image != null)
            {
                image.fillAmount = fillAmount;
            }
            return image;
        }

        #endregion

        #region 可见性控制

        /// <summary>
        /// 设置对象可见性
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">对象路径</param>
        /// <param name="visible">是否可见</param>
        public static void SetActive(this Transform root, string path, bool visible)
        {
            var child = root.Find(path);
            if (child != null)
            {
                child.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置CanvasGroup可见性
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="path">对象路径</param>
        /// <param name="visible">是否可见</param>
        /// <param name="interactable">是否可交互</param>
        public static void SetCanvasGroupVisible(this Transform root, string path, bool visible, bool interactable = true)
        {
            var child = root.Find(path);
            if (child != null)
            {
                var canvasGroup = child.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = child.gameObject.AddComponent<CanvasGroup>();

                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible && interactable;
                canvasGroup.blocksRaycasts = visible && interactable;
            }
        }

        #endregion

        #region 自动绑定

        /// <summary>
        /// 自动绑定带有[UIBind]特性的字段
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="root">UI根节点</param>
        public static void AutoBind(object target, Transform root)
        {
            if (target == null || root == null)
                return;

            var type = target.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var bindAttr = field.GetCustomAttribute<UIBindAttribute>();
                if (bindAttr == null)
                    continue;

                var path = string.IsNullOrEmpty(bindAttr.Path) ? field.Name : bindAttr.Path;
                var component = FindComponentByType(root, path, field.FieldType);

                if (component != null)
                {
                    field.SetValue(target, component);
                    Debug.Log($"[UIBinder] 自动绑定: {field.Name} -> {path}");
                }
                else
                {
                    Debug.LogWarning($"[UIBinder] 绑定失败: {field.Name} -> {path}");
                }
            }
        }

        private static Component FindComponentByType(Transform root, string path, Type componentType)
        {
            var child = root.Find(path);
            if (child == null)
                child = root.FindChildRecursive(path);

            if (child == null)
                return null;

            return child.GetComponent(componentType);
        }

        #endregion
    }

    /// <summary>
    /// UI绑定特性
    /// 用于标记需要自动绑定的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class UIBindAttribute : Attribute
    {
        /// <summary>
        /// UI路径（为空时使用字段名）
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="path">UI路径</param>
        public UIBindAttribute(string path = null)
        {
            Path = path;
        }
    }

    /// <summary>
    /// UI引用缓存
    /// 用于缓存频繁访问的UI组件引用
    /// </summary>
    public class UIReferenceCache
    {
        private readonly Transform _root;
        private readonly Dictionary<string, Component> _cache = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="root">UI根节点</param>
        public UIReferenceCache(Transform root)
        {
            _root = root;
        }

        /// <summary>
        /// 获取组件（带缓存）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">组件路径</param>
        /// <returns>组件实例</returns>
        public T Get<T>(string path) where T : Component
        {
            var key = $"{typeof(T).Name}:{path}";

            if (_cache.TryGetValue(key, out var cached))
                return cached as T;

            var component = _root.FindComponent<T>(path);
            if (component != null)
                _cache[key] = component;

            return component;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 移除指定路径的缓存
        /// </summary>
        /// <param name="path">组件路径</param>
        public void Remove(string path)
        {
            var keysToRemove = new List<string>();
            foreach (var key in _cache.Keys)
            {
                if (key.EndsWith($":{path}"))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
                _cache.Remove(key);
        }
    }
}
