using UnityEngine;

namespace xFrame.StateMachine.Examples
{
    /// <summary>
    /// 玩家待机状态
    /// </summary>
    public class PlayerIdleState : StateBase<PlayerContext>
    {
        /// <summary>
        /// 进入待机状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnEnter(PlayerContext context)
        {
            Debug.Log("[PlayerIdleState] 进入待机状态");
        }

        /// <summary>
        /// 更新待机状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnUpdate(PlayerContext context)
        {
            // 待机状态逻辑
            // 例如：播放待机动画
        }

        /// <summary>
        /// 退出待机状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnExit(PlayerContext context)
        {
            Debug.Log("[PlayerIdleState] 退出待机状态");
        }
    }
}
