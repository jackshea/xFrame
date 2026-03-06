# xFrame UI框架示例

本目录包含UI框架的完整示例代码，展示如何使用xFrame UI系统。

## 文件说明

### 核心接口和基类
- `IUIManager.cs` - UI管理器接口定义
- `UIView.cs` - UI视图基类
- `UIPanel.cs` - UI面板基类
- `UIWindow.cs` - UI窗口基类
- `UILayer.cs` - UI层级枚举

### 管理器实现
- `UIManager.cs` - UI管理器实现
- `UIManagerModule.cs` - UI模块（集成到xFrame）
- `UILayerManager.cs` - UI层级管理器
- `UINavigationStack.cs` - UI导航栈管理器

### 示例UI
- `Examples/MainMenuPanel.cs` - 主菜单面板示例
- `Examples/ConfirmDialog.cs` - 确认对话框示例
- `Examples/PlayerInfoPanel.cs` - 玩家信息面板示例（MVVM模式）
- `Examples/LoadingPanel.cs` - 加载界面示例

### UI事件
- `Events/UIEvents.cs` - UI相关事件定义

### 扩展
- `Extensions/UIManagerExtensions.cs` - UI管理器扩展方法

## 快速开始

### 1. 集成到项目

在`xFrameLifetimeScope.cs`中注册UI系统：

```csharp
private void RegisterUISystem(IContainerBuilder builder)
{
    builder.Register<IUIManager, UIManager>(Lifetime.Singleton);
    builder.Register<UIManagerModule>(Lifetime.Singleton)
        .AsImplementedInterfaces()
        .AsSelf();
}
```

### 2. 创建UI预制体

1. 在Unity中创建UI预制体
2. 添加Canvas组件
3. 添加继承自`UIPanel`或`UIWindow`的脚本
4. 标记为Addressable资源，地址格式：`UI/{脚本名称}`

### 3. 使用UI

```csharp
public class GameController : MonoBehaviour
{
    [Inject]
    private IUIManager _uiManager;
    
    async void Start()
    {
        await _uiManager.OpenAsync<MainMenuPanel>();
    }
}
```

## 注意事项

1. 确保场景中有`EventSystem`组件
2. UI预制体需要配置为Addressable资源
3. UI脚本必须在VContainer的生命周期范围内
4. UI操作必须在主线程进行

## 参考文档

详细文档请查看：`Assets/xFrame/Docs/UI框架.md`
