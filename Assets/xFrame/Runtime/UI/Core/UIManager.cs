using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime.ResourceManager;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI管理器实现
    /// 负责所有UI的加载、显示、隐藏、销毁等管理
    /// 单例模式，通过 UIManager.Instance 访问
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager _instance;

        // 各层级的Canvas根节点
        private readonly Dictionary<UILayer, Transform> _layerRoots = new();

        // UI导航栈
        private readonly Stack<UIView> _navigationStack = new();

        // 当前已打开的UI实例（按类型存储）
        private readonly Dictionary<Type, UIView> _openedUIs = new();

        // 预加载的UI资源（按类型存储）
        private readonly Dictionary<Type, GameObject> _preloadedPrefabs = new();

        // UI对象池（按类型存储）
        private readonly Dictionary<Type, Queue<UIView>> _uiPools = new();

        /// <summary>
        /// 资源管理器
        /// </summary>
        private IAssetManager _assetManager;

        // Canvas Scaler引用
        private CanvasScaler _canvasScaler;

        // UI根节点
        private Transform _uiRoot;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试在场景中查找
                    _instance = FindObjectOfType<UIManager>();

                    if (_instance == null)
                    {
                        // 创建新的UIManager实例
                        var go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAssetManager();

            InitializeUIRoot();
        }

        #region Unity生命周期

        /// <summary>
        /// 当对象被销毁时清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;

            CloseAll();

            // 清理所有UI资源
            ClearAllUIResources();

            // 清理对象池
            foreach (var pool in _uiPools.Values)
                while (pool.Count > 0)
                {
                    var ui = pool.Dequeue();
                    if (ui != null)
                    {
                        ui.InternalOnDestroy();
                        Destroy(ui.gameObject);
                    }
                }

            _uiPools.Clear();
            _preloadedPrefabs.Clear();
        }

        #endregion

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        private void InitializeAssetManager()
        {
            // 尝试从VContainer获取IAssetManager实例
            try
            {
                // 尝试通过GameObject上的LifetimeScope组件获取容器
                var lifetimeScope = GetComponent<LifetimeScope>();
                if (lifetimeScope != null)
                {
                    _assetManager = lifetimeScope.Container.Resolve<IAssetManager>();
                }
                else
                {
                    // 如果当前GameObject上没有LifetimeScope，尝试查找场景中的
                    lifetimeScope = FindObjectOfType<LifetimeScope>();
                    if (lifetimeScope != null) _assetManager = lifetimeScope.Container.Resolve<IAssetManager>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIManager] 无法通过VContainer获取资源管理器: {ex.Message}");
                // 如果无法通过VContainer获取，则创建默认实例
                _assetManager = new AddressableAssetManager();
            }

            // 如果仍然没有资源管理器，则使用默认实现
            if (_assetManager == null)
            {
                Debug.LogWarning("[UIManager] 使用默认资源管理器");
                _assetManager = new AddressableAssetManager();
            }

            Debug.Log("[UIManager] 资源管理器初始化完成");
        }

        /// <summary>
        /// 初始化UI根节点和各层级Canvas
        /// </summary>
        private void InitializeUIRoot()
        {
            _uiRoot = transform;

            // 为每个层级创建Canvas
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer))) CreateLayerCanvas(layer);
        }

        /// <summary>
        /// 创建指定层级的Canvas
        /// </summary>
        private void CreateLayerCanvas(UILayer layer)
        {
            var canvasGO = new GameObject(layer.GetCanvasName());
            canvasGO.transform.SetParent(_uiRoot, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = layer.GetBaseSortOrder();

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            _layerRoots[layer] = canvasGO.transform;
        }

        /// <summary>
        /// 释放UI预制体资源
        /// </summary>
        private void ReleaseUIPrefab(UIView uiView)
        {
            if (_assetManager == null || uiView == null) return;

            // 注意：这里我们不直接释放预制体，因为可能还有其他实例在使用
            // 实际项目中可能需要更复杂的资源管理策略
            // 这里仅作为示例展示如何使用资源管理器

            // 可以考虑在适当的时候批量释放未使用的资源
            // 例如：_assetManager.ReleaseAsset(assetAddress);
        }

        #region 打开UI

        /// <summary>
        /// 异步打开UI
        /// </summary>
        public async Task<T> OpenAsync<T>(object data = null) where T : UIView
        {
            return await OpenAsync(typeof(T), data) as T;
        }

        /// <summary>
        /// 异步打开UI（通过类型）
        /// </summary>
        public async Task<UIView> OpenAsync(Type uiType, object data = null)
        {
            if (uiType == null || !typeof(UIView).IsAssignableFrom(uiType))
            {
                Debug.LogError($"无效的UI类型: {uiType?.Name}");
                return null;
            }

            // 检查是否已打开
            if (_openedUIs.TryGetValue(uiType, out var existingUI))
            {
                Debug.LogWarning($"UI {uiType.Name} 已经打开");
                return existingUI;
            }

            // 获取或创建UI实例
            var uiInstance = await GetOrCreateUIInstance(uiType);

            if (uiInstance == null)
            {
                Debug.LogError($"无法创建UI实例: {uiType.Name}");
                return null;
            }

            // 设置父节点
            var parentTransform = _layerRoots[uiInstance.Layer];
            uiInstance.transform.SetParent(parentTransform, false);

            // 调用生命周期
            uiInstance.InternalOnCreate();
            uiInstance.InternalOnOpen(data);

            // 添加到已打开列表
            _openedUIs[uiType] = uiInstance;

            // 如果是窗口且使用导航栈，添加到栈中
            if (uiInstance is UIWindow window && window.UseStack)
            {
                // 隐藏栈顶UI
                if (_navigationStack.Count > 0)
                {
                    var topUI = _navigationStack.Peek();
                    topUI.InternalOnHide();
                }

                _navigationStack.Push(uiInstance);
            }

            // 设置Canvas排序
            UpdateCanvasSortOrder(uiInstance);

            return uiInstance;
        }

        /// <summary>
        /// 获取或创建UI实例
        /// </summary>
        private async Task<UIView> GetOrCreateUIInstance(Type uiType)
        {
            // 尝试从对象池获取
            if (_uiPools.TryGetValue(uiType, out var pool) && pool.Count > 0)
            {
                var pooledUI = pool.Dequeue();
                pooledUI.OnGet();
                return pooledUI;
            }

            // 从预加载缓存获取
            if (_preloadedPrefabs.TryGetValue(uiType, out var prefab))
                return Instantiate(prefab).GetComponent<UIView>();

            // 加载UI预制体
            var loadedPrefab = await LoadUIPrefabAsync(uiType);

            if (loadedPrefab == null)
            {
                Debug.LogError($"无法加载UI预制体: {uiType.Name}");
                return null;
            }

            return Instantiate(loadedPrefab).GetComponent<UIView>();
        }

        /// <summary>
        /// 异步加载UI预制体
        /// 使用IAssetManager加载资源
        /// </summary>
        private async Task<GameObject> LoadUIPrefabAsync(Type uiType)
        {
            // 构造资源地址
            var assetAddress = $"UI/{uiType.Name}";

            try
            {
                // 使用资源管理器异步加载
                var prefab = await _assetManager.LoadAssetAsync<GameObject>(assetAddress);

                if (prefab == null)
                {
                    Debug.LogError($"[UIManager] 无法通过资源管理器加载UI预制体: {assetAddress}");

                    // 回退到Resources加载
                    prefab = Resources.Load<GameObject>(assetAddress);

                    if (prefab == null) Debug.LogError($"[UIManager] 无法从Resources加载UI预制体: {assetAddress}");
                }

                return prefab;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIManager] 加载UI预制体异常: {assetAddress}, 错误: {ex.Message}");

                // 回退到Resources加载
                try
                {
                    var prefab = Resources.Load<GameObject>(assetAddress);
                    if (prefab == null) Debug.LogError($"[UIManager] 回退加载失败，无法从Resources加载UI预制体: {assetAddress}");
                    return prefab;
                }
                catch (Exception fallbackEx)
                {
                    Debug.LogError($"[UIManager] 回退加载异常: {assetAddress}, 错误: {fallbackEx.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 更新Canvas排序顺序
        /// </summary>
        private void UpdateCanvasSortOrder(UIView uiView)
        {
            if (uiView.Canvas != null)
            {
                var layer = uiView.Layer;
                var baseSortOrder = layer.GetBaseSortOrder();

                // 计算同层级UI数量，作为偏移量
                var sameLayerCount = _openedUIs.Values.Count(ui => ui.Layer == layer);
                uiView.Canvas.overrideSorting = true;
                uiView.Canvas.sortingOrder = baseSortOrder + sameLayerCount;
            }
        }

        #endregion

        #region 关闭UI

        /// <summary>
        /// 关闭指定类型的UI
        /// </summary>
        public void Close<T>() where T : UIView
        {
            Close(typeof(T));
        }

        /// <summary>
        /// 关闭指定实例的UI
        /// </summary>
        public void Close(UIView view)
        {
            if (view == null) return;

            Close(view.GetType());
        }

        /// <summary>
        /// 关闭指定类型的UI
        /// </summary>
        public void Close(Type uiType)
        {
            if (!_openedUIs.TryGetValue(uiType, out var uiInstance))
            {
                Debug.LogWarning($"UI {uiType.Name} 未打开，无法关闭");
                return;
            }

            // 从导航栈移除
            if (_navigationStack.Contains(uiInstance))
            {
                // 如果是栈顶元素，直接弹出
                if (_navigationStack.Peek() == uiInstance)
                {
                    _navigationStack.Pop();

                    // 显示栈中的下一个UI
                    if (_navigationStack.Count > 0)
                    {
                        var nextUI = _navigationStack.Peek();
                        nextUI.InternalOnShow();
                    }
                }
                else
                {
                    // 如果不是栈顶，需要重建栈
                    var tempStack = new Stack<UIView>();
                    while (_navigationStack.Count > 0)
                    {
                        var ui = _navigationStack.Pop();
                        if (ui != uiInstance) tempStack.Push(ui);
                    }

                    while (tempStack.Count > 0) _navigationStack.Push(tempStack.Pop());
                }
            }

            // 调用关闭生命周期
            uiInstance.InternalOnClose();

            // 从已打开列表移除
            _openedUIs.Remove(uiType);

            // 如果可缓存，放入对象池
            if (uiInstance.Cacheable)
            {
                ReturnToPool(uiInstance);
            }
            else
            {
                // 销毁UI实例
                uiInstance.InternalOnDestroy();

                // 尝试释放UI预制体资源
                ReleaseUIPrefab(uiInstance);

                Destroy(uiInstance.gameObject);
            }
        }

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        public void CloseAll(UILayer? layer = null)
        {
            var uisToClose = layer.HasValue
                ? _openedUIs.Values.Where(ui => ui.Layer == layer.Value).ToList()
                : _openedUIs.Values.ToList();

            foreach (var ui in uisToClose) Close(ui.GetType());
        }

        /// <summary>
        /// 将UI实例返回到对象池
        /// </summary>
        private void ReturnToPool(UIView uiInstance)
        {
            var uiType = uiInstance.GetType();

            if (!_uiPools.ContainsKey(uiType)) _uiPools[uiType] = new Queue<UIView>();

            uiInstance.OnRelease();
            _uiPools[uiType].Enqueue(uiInstance);
        }

        #endregion

        #region 查询UI

        /// <summary>
        /// 获取已打开的UI实例
        /// </summary>
        public T Get<T>() where T : UIView
        {
            return Get(typeof(T)) as T;
        }

        /// <summary>
        /// 获取已打开的UI实例（通过类型）
        /// </summary>
        public UIView Get(Type uiType)
        {
            _openedUIs.TryGetValue(uiType, out var uiInstance);
            return uiInstance;
        }

        /// <summary>
        /// 检查指定类型的UI是否已打开
        /// </summary>
        public bool IsOpen<T>() where T : UIView
        {
            return IsOpen(typeof(T));
        }

        /// <summary>
        /// 检查指定类型的UI是否已打开
        /// </summary>
        public bool IsOpen(Type uiType)
        {
            return _openedUIs.ContainsKey(uiType);
        }

        #endregion

        #region 导航栈

        /// <summary>
        /// 返回上一个UI（栈管理）
        /// </summary>
        public void Back()
        {
            if (_navigationStack.Count == 0)
            {
                Debug.LogWarning("导航栈为空，无法返回");
                return;
            }

            var currentUI = _navigationStack.Pop();
            Close(currentUI.GetType());

            // 显示栈中的下一个UI
            if (_navigationStack.Count > 0)
            {
                var previousUI = _navigationStack.Peek();
                previousUI.InternalOnShow();
            }
        }

        /// <summary>
        /// 检查是否可以返回
        /// </summary>
        public bool CanGoBack()
        {
            return _navigationStack.Count > 0;
        }

        /// <summary>
        /// 清空导航栈
        /// </summary>
        public void ClearNavigationStack()
        {
            while (_navigationStack.Count > 0)
            {
                var ui = _navigationStack.Pop();
                Close(ui.GetType());
            }
        }

        #endregion

        #region 预加载

        /// <summary>
        /// 预加载UI资源
        /// </summary>
        public async Task PreloadAsync<T>() where T : UIView
        {
            await PreloadAsync(typeof(T));
        }

        /// <summary>
        /// 预加载UI资源（通过类型）
        /// </summary>
        public async Task PreloadAsync(Type uiType)
        {
            if (_preloadedPrefabs.ContainsKey(uiType))
            {
                Debug.LogWarning($"UI {uiType.Name} 已经预加载");
                return;
            }

            // 构造资源地址
            var assetAddress = $"UI/{uiType.Name}";

            try
            {
                // 使用资源管理器预加载
                await _assetManager.PreloadAssetAsync(assetAddress);

                // 加载预制体到缓存
                var prefab = await _assetManager.LoadAssetAsync<GameObject>(assetAddress);

                if (prefab != null)
                {
                    _preloadedPrefabs[uiType] = prefab;
                    Debug.Log($"预加载UI成功: {uiType.Name}");
                }
                else
                {
                    Debug.LogError($"预加载UI失败: {uiType.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"预加载UI异常: {uiType.Name}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 预加载多个UI资源
        /// </summary>
        public async Task PreloadBatchAsync(params Type[] uiTypes)
        {
            var tasks = new List<Task>();

            foreach (var uiType in uiTypes) tasks.Add(PreloadAsync(uiType));

            await Task.WhenAll(tasks);
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 释放未使用的UI资源
        /// </summary>
        public void ReleaseUnusedResources()
        {
            if (_assetManager == null) return;

            // 在实际项目中，这里可以实现更复杂的资源释放逻辑
            // 例如：检查哪些预制体长时间未使用并释放它们

            Debug.Log("[UIManager] 资源释放完成");
        }

        /// <summary>
        /// 清理所有缓存的UI资源
        /// </summary>
        public void ClearAllUIResources()
        {
            if (_assetManager == null) return;

            // 清理预加载的预制体缓存
            _preloadedPrefabs.Clear();

            // 清理对象池
            foreach (var pool in _uiPools.Values)
                while (pool.Count > 0)
                {
                    var ui = pool.Dequeue();
                    if (ui != null)
                    {
                        ui.InternalOnDestroy();
                        Destroy(ui.gameObject);
                    }
                }

            _uiPools.Clear();

            Debug.Log("[UIManager] 所有UI资源已清理");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 设置指定层级的交互性
        /// </summary>
        public void SetLayerInteractable(UILayer layer, bool interactable)
        {
            if (!_layerRoots.TryGetValue(layer, out var layerRoot))
            {
                Debug.LogWarning($"层级 {layer} 不存在");
                return;
            }

            var canvasGroup = layerRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = layerRoot.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        /// <summary>
        /// 获取指定层级当前打开的UI数量
        /// </summary>
        public int GetOpenUICount(UILayer layer)
        {
            return _openedUIs.Values.Count(ui => ui.Layer == layer);
        }

        #endregion
    }
}