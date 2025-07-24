# xFrame 日志系统 (Logging System)

## 概述

xFrame日志系统是一个功能强大、线程安全、高性能的日志记录模块，专为Unity项目设计。它提供了统一的日志接口，支持多种输出通道，并与VContainer依赖注入框架深度集成。

## 主要特性

- **六级日志等级**: Verbose, Debug, Info, Warning, Error, Fatal
- **多输出通道**: 控制台、文件、Unity Debug、网络输出
- **线程安全**: 支持多线程环境下的并发日志记录
- **格式化输出**: 包含时间戳、线程ID、模块名等详细信息
- **运行时配置**: 支持动态调整日志等级和输出器开关
- **异常捕获**: 自动捕获Unity异常和应用程序崩溃日志
- **VContainer集成**: 完全集成到依赖注入框架中
- **高性能**: 优化的日志写入性能，最小化对主程序的影响

## 快速开始

### 1. 基本使用

```csharp
using xFrame.Core.Logging;
using VContainer;

public class MyGameModule : MonoBehaviour
{
    [Inject] private ILogManager _logManager;
    private ILogger _logger;

    private void Start()
    {
        // 获取当前类的日志记录器
        _logger = _logManager.GetLogger<MyGameModule>();
        
        // 记录不同等级的日志
        _logger.Info("游戏模块初始化完成");
        _logger.Warning("这是一个警告消息");
        _logger.Error("发生了一个错误");
    }
}
```

### 2. 静态访问方式

```csharp
using xFrame.Core.Logging;

public class AnywhereInYourCode
{
    public void SomeMethod()
    {
        // 使用静态接口记录日志
        Log.Info("这是一条信息日志");
        Log.Error("这是一条错误日志", "ModuleName");
        
        // 获取特定类型的Logger
        var logger = Log.GetLogger<AnywhereInYourCode>();
        logger.Debug("使用特定Logger记录调试信息");
    }
}
```

### 3. 异常日志记录

```csharp
try
{
    // 可能抛出异常的代码
    DoSomethingRisky();
}
catch (Exception ex)
{
    // 记录带异常信息的日志
    _logger.Error("操作失败", ex);
    Log.Fatal("严重错误", ex, "CriticalModule");
}
```

## 配置说明

### 1. VContainer注册

日志系统已经在 `xFrameLifetimeScope` 中自动注册，无需额外配置：

```csharp
// 自动注册的服务
- ILogManager (单例)
- LoggingModule (单例，实现IInitializable)
- ILogger工厂方法
```

### 2. 自定义配置

可以通过 `LoggingConfiguration` 类进行详细配置：

```csharp
var config = new LoggingConfiguration
{
    globalMinLevel = LogLevel.Info,
    enableUnityDebug = true,
    enableFile = true,
    enableNetwork = false,
    dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff"
};
```

### 3. 预定义配置

```csharp
// 开发环境配置
var devConfig = LoggingConfiguration.CreateDevelopment();

// 生产环境配置
var prodConfig = LoggingConfiguration.CreateProduction();

// 默认配置
var defaultConfig = LoggingConfiguration.CreateDefault();
```

## 输出器 (Appenders)

### 1. Unity Debug输出器
- 输出到Unity控制台
- 支持不同日志等级的颜色区分
- 自动处理异常信息

### 2. 控制台输出器
- 输出到系统控制台
- 支持彩色输出
- 适用于独立构建版本

### 3. 文件输出器
- 输出到文件系统
- 自动创建日志目录
- 支持自定义文件路径
- 线程安全的文件写入

### 4. 网络输出器
- 通过HTTP发送到远程服务器
- 批量发送优化性能
- 自动重试机制
- 适用于远程日志收集

## 日志等级

| 等级 | 数值 | 用途 | 示例 |
|------|------|------|------|
| Verbose | 0 | 最详细的调试信息 | 变量值、执行路径 |
| Debug | 1 | 开发调试信息 | 函数调用、状态变化 |
| Info | 2 | 一般运行信息 | 系统启动、功能完成 |
| Warning | 3 | 警告信息 | 配置缺失、性能问题 |
| Error | 4 | 错误信息 | 操作失败、异常处理 |
| Fatal | 5 | 严重错误 | 系统崩溃、致命异常 |

## 性能优化

### 1. 条件日志记录

```csharp
// 避免不必要的字符串构造
if (_logger.IsLevelEnabled(LogLevel.Debug))
{
    var expensiveInfo = GenerateExpensiveDebugInfo();
    _logger.Debug($"调试信息: {expensiveInfo}");
}
```

### 2. 日志等级过滤

```csharp
// 设置最小日志等级，过滤低等级日志
_logger.MinLevel = LogLevel.Warning;
```

### 3. 输出器配置

```csharp
// 为不同输出器设置不同的最小等级
unityDebugAppender.MinLevel = LogLevel.Debug;
fileAppender.MinLevel = LogLevel.Info;
networkAppender.MinLevel = LogLevel.Error;
```

## 线程安全

日志系统在设计时充分考虑了线程安全：

- 所有日志写入操作都使用锁保护
- 支持多线程并发日志记录
- 网络输出器使用并发队列优化性能
- 文件输出器确保线程安全的文件写入

## 异常处理

### 1. 自动异常捕获

系统会自动捕获Unity的异常和日志消息：

```csharp
// Unity的Debug.LogError会被自动捕获并转换为日志系统的Error级别
Debug.LogError("Unity错误消息");
```

### 2. 手动异常记录

```csharp
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    _logger.Error("操作失败", ex);
}
```

## 示例项目

查看 `Examples/Logging` 文件夹中的完整示例：

- `LoggingExample.cs` - 基本使用示例
- `LoggingExampleLifetimeScope.cs` - 自定义配置示例
- `LoggingTestRunner.cs` - 自动化测试示例

## 最佳实践

### 1. Logger获取

```csharp
// 推荐：为每个类创建专用的Logger
private readonly ILogger _logger;

public MyClass(ILogManager logManager)
{
    _logger = logManager.GetLogger<MyClass>();
}
```

### 2. 日志消息格式

```csharp
// 好的日志消息
_logger.Info("用户登录成功，用户ID: {0}, 登录时间: {1}", userId, loginTime);

// 避免的日志消息
_logger.Info("something happened"); // 信息不够具体
```

### 3. 异常处理

```csharp
// 记录异常时提供上下文信息
catch (Exception ex)
{
    _logger.Error($"处理用户请求失败，用户ID: {userId}, 请求类型: {requestType}", ex);
}
```

### 4. 性能考虑

```csharp
// 对于复杂的日志消息，使用条件检查
if (_logger.IsLevelEnabled(LogLevel.Debug))
{
    _logger.Debug($"复杂的调试信息: {ExpensiveOperation()}");
}
```

## 故障排除

### 1. 日志不显示

- 检查日志等级设置
- 确认输出器是否启用
- 验证VContainer注册是否正确

### 2. 文件日志写入失败

- 检查文件路径权限
- 确认磁盘空间充足
- 查看Unity控制台的错误信息

### 3. 网络日志发送失败

- 验证网络连接
- 检查服务器端点配置
- 查看网络输出器的错误日志

## API参考

详细的API文档请参考各个接口和类的XML注释。主要接口包括：

- `ILogger` - 日志记录器接口
- `ILogManager` - 日志管理器接口
- `ILogAppender` - 日志输出器接口
- `ILogFormatter` - 日志格式化器接口
- `LoggingModule` - 日志模块主类
- `Log` - 静态访问接口
