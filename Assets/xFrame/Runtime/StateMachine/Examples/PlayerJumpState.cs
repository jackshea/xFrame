using UnityEngine;

namespace xFrame.StateMachine.Examples
{
    /// <summary>
    /// 玩家跳跃状态
    /// </summary>
    public class PlayerJumpState : StateBase<PlayerContext>
    {
        private float _jumpForce = 10f;
        private Rigidbody _rigidbody;

        /// <summary>
        /// 进入跳跃状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnEnter(PlayerContext context)
        {
            Debug.Log("[PlayerJumpState] 进入跳跃状态");

            if (context.GameObject != null && context.IsGrounded)
            {
                _rigidbody = context.GameObject.GetComponent<Rigidbody>();
                if (_rigidbody != null)
                {
                    _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                    context.IsGrounded = false;
                }
            }
        }

        /// <summary>
        /// 更新跳跃状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnUpdate(PlayerContext context)
        {
            // 跳跃状态逻辑
            // 例如：检测是否落地
            if (_rigidbody != null && _rigidbody.velocity.y <= 0)
            {
                // 可以在这里检测地面碰撞
                // 如果落地，设置 context.IsGrounded = true
            }
        }

        /// <summary>
        /// 退出跳跃状态
        /// </summary>
        /// <param name="context">玩家上下文</param>
        public override void OnExit(PlayerContext context)
        {
            Debug.Log("[PlayerJumpState] 退出跳跃状态");
        }
    }
}
