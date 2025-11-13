using UnityEngine;
using VContainer;

namespace xFrame.Runtime
{
    /// <summary>
    /// 模块基类
    /// 提供模块基本功能的抽象实现
    /// </summary>
    public abstract class BaseModule : IModule
    {
        /// <summary>
        /// 依赖注入容器
        /// </summary>
        protected IObjectResolver Container { get; private set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public abstract string ModuleName { get; }

        /// <summary>
        /// 模块优先级，数字越小优先级越高
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// 模块初始化
        /// 在模块被加载时调用，用于初始化模块的基本设置
        /// </summary>
        public virtual void OnInit()
        {
            Debug.Log($"[{ModuleName}] 模块初始化");
        }

        /// <summary>
        /// 模块启动
        /// 在所有模块初始化完成后调用，可以依赖其他模块进行操作
        /// </summary>
        public virtual void OnStart()
        {
            Debug.Log($"[{ModuleName}] 模块启动");
        }

        /// <summary>
        /// 模块销毁
        /// 在模块被卸载或程序退出时调用，用于清理资源
        /// </summary>
        public virtual void OnDestroy()
        {
            Debug.Log($"[{ModuleName}] 模块销毁");
        }

        /// <summary>
        /// 设置依赖注入容器
        /// </summary>
        /// <param name="container">VContainer依赖注入容器</param>
        public void SetContainer(IObjectResolver container)
        {
            Container = container;
        }
    }
}