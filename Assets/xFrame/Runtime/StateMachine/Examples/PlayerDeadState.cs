using UnityEngine;

namespace xFrame.StateMachine.Examples
{
    /// <summary>
    /// 玩家死亡状态
    /// </summary>
    public class PlayerDeadState : StateBase<PlayerContext>
    {
        /// <summary>
        /// 进入死亡状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnEnter(PlayerContext context)
        {
            Debug.Log("[PlayerDeadState] 进入死亡状态");
            context.Health = 0;
            // 播放死亡动画
            // 禁用玩家控制
        }

        /// <summary>
        /// 更新死亡状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnUpdate(PlayerContext context)
        {
            // 死亡状态逻辑
            // 例如：等待重生
        }

        /// <summary>
        /// 退出死亡状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnExit(PlayerContext context)
        {
            Debug.Log("[PlayerDeadState] 退出死亡状态");
            // 重置玩家状态
            context.Health = 100f;
        }
    }
}
