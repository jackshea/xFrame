using UnityEngine;
using xFrame.Core;

namespace xFrame.Examples
{
    /// <summary>
    /// 示例模块
    /// 演示如何创建和使用模块系统
    /// </summary>
    public class ExampleModule : BaseModule, IUpdatableModule
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        public override string ModuleName => "示例模块";

        /// <summary>
        /// 模块优先级，数值越小优先级越高
        /// </summary>
        public override int Priority => 10;

        private float _timer = 0f;

        /// <summary>
        /// 模块初始化
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            Debug.Log($"[{ModuleName}] 自定义初始化逻辑");
        }

        /// <summary>
        /// 模块启动
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            Debug.Log($"[{ModuleName}] 自定义启动逻辑");
            
            // 可以使用Container获取其他依赖
            // var someService = Container.Resolve<ISomeService>();
        }

        /// <summary>
        /// 模块更新
        /// 每帧调用一次
        /// </summary>
        public void OnUpdate()
        {
            // 每隔2秒输出一条日志
            _timer += Time.deltaTime;
            if (_timer >= 2f)
            {
                Debug.Log($"[{ModuleName}] 更新中...");
                _timer = 0f;
            }
        }

        /// <summary>
        /// 模块销毁
        /// </summary>
        public override void OnDestroy()
        {
            Debug.Log($"[{ModuleName}] 自定义销毁逻辑");
            base.OnDestroy();
        }
    }
}
