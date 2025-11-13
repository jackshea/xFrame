using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace xFrame.Runtime.MessagePipe
{
    /// <summary>
    /// MessagePipe事件系统模块
    /// 负责初始化和管理MessagePipe的全局配置
    /// </summary>
    public class MessagePipeModule : IInitializable, IModule
    {
        /// <summary>
        /// 容器解析器引用
        /// </summary>
        private IObjectResolver resolver;

        /// <summary>
        /// 模块是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// VContainer的IInitializable接口实现
        /// 在容器构建完成后自动调用
        /// </summary>
        public void Initialize()
        {
            // 注意：这个方法在VContainer中不会被自动调用
            // 我们使用ModuleRegistry来管理模块初始化
            Debug.LogWarning("[MessagePipeModule] VContainer IInitializable被调用，请使用Initialize(IObjectResolver)方法");
        }

        /// <summary>
        /// 获取模块名称
        /// </summary>
        public string ModuleName => "MessagePipe";

        /// <summary>
        /// 模块优先级（数值越小优先级越高）
        /// MessagePipe需要在其他模块之前初始化
        /// </summary>
        public int Priority => 10;

        /// <summary>
        /// 模块初始化
        /// 在模块被加载时调用，用于初始化模块的基本设置
        /// </summary>
        public void OnInit()
        {
            Debug.Log("[MessagePipeModule] 模块初始化开始");
            // 这里可以进行不需要依赖注入的初始化工作
        }

        /// <summary>
        /// 模块启动
        /// 在所有模块初始化完成后调用，可以依赖其他模块进行操作
        /// </summary>
        public void OnStart()
        {
            Debug.Log("[MessagePipeModule] 模块启动");
            // 这里可以进行需要依赖其他模块的操作
        }

        /// <summary>
        /// 模块销毁
        /// 在模块被卸载或程序退出时调用，用于清理资源
        /// </summary>
        public void OnDestroy()
        {
            Shutdown();
        }

        /// <summary>
        /// 初始化MessagePipe模块
        /// </summary>
        /// <param name="container">依赖注入容器</param>
        public void Initialize(IObjectResolver container)
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[MessagePipeModule] 模块已经初始化，跳过重复初始化");
                return;
            }

            resolver = container;

            try
            {
                // 设置全局MessagePipe提供程序
                // 这对于Unity编辑器中的诊断窗口是必需的
                GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());

                IsInitialized = true;
                Debug.Log("[MessagePipeModule] MessagePipe事件系统初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessagePipeModule] 初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 关闭模块
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized) return;

            try
            {
                // 清理全局MessagePipe提供程序
                GlobalMessagePipe.SetProvider(null);

                IsInitialized = false;
                Debug.Log("[MessagePipeModule] MessagePipe事件系统已关闭");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessagePipeModule] 关闭时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取模块状态信息
        /// </summary>
        /// <returns>模块状态字符串</returns>
        public string GetStatus()
        {
            return $"MessagePipe模块 - 初始化状态: {(IsInitialized ? "已初始化" : "未初始化")}";
        }
    }
}