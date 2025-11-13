using System;
using UnityEngine;
using VContainer.Unity;

namespace xFrame.Core
{
    /// <summary>
    /// 模块接口
    /// 定义框架中模块的基本行为和生命周期
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// 获取模块名称
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// 模块优先级，数字越小优先级越高
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 模块初始化
        /// 在模块被加载时调用，用于初始化模块的基本设置
        /// </summary>
        void OnInit();

        /// <summary>
        /// 模块启动
        /// 在所有模块初始化完成后调用，可以依赖其他模块进行操作
        /// </summary>
        void OnStart();

        /// <summary>
        /// 模块销毁
        /// 在模块被卸载或程序退出时调用，用于清理资源
        /// </summary>
        void OnDestroy();
    }
}
