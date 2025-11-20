using UnityEngine;
using xFrame.Runtime.StateMachine;

namespace xFrame.Examples.StateMachine
{
    /// <summary>
    /// 玩家移动状态
    /// </summary>
    public class PlayerMoveState : StateBase<PlayerContext>
    {
        /// <summary>
        /// 进入移动状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnEnter(PlayerContext context)
        {
            Debug.Log("[PlayerMoveState] 进入移动状态");
        }

        /// <summary>
        /// 更新移动状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnUpdate(PlayerContext context)
        {
            // 移动状态逻辑
            // 例如：根据输入移动玩家
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (context.GameObject != null)
            {
                Vector3 movement = new Vector3(horizontal, 0, vertical) * context.MoveSpeed * Time.deltaTime;
                context.GameObject.transform.Translate(movement);
            }
        }

        /// <summary>
        /// 退出移动状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnExit(PlayerContext context)
        {
            Debug.Log("[PlayerMoveState] 退出移动状态");
        }
    }
}
