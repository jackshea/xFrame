using UnityEngine;

namespace xFrame.Examples.StateMachine
{
    /// <summary>
    /// 玩家状态机上下文示例
    /// </summary>
    public class PlayerContext
    {
        /// <summary>
        /// 玩家游戏对象
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// 玩家生命值
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// 玩家移动速度
        /// </summary>
        public float MoveSpeed { get; set; }

        /// <summary>
        /// 玩家是否在地面上
        /// </summary>
        public bool IsGrounded { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="gameObject">玩家游戏对象</param>
        public PlayerContext(GameObject gameObject)
        {
            GameObject = gameObject;
            Health = 100f;
            MoveSpeed = 5f;
            IsGrounded = true;
        }
    }
}
