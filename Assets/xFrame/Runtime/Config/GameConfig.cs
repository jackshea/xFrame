using UnityEngine;

namespace xFrame.Config
{
    /// <summary>
    /// 游戏通用配置示例。
    /// 演示如何继承 BaseConfig 并定义具体的数据字段。
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameConfig", menuName = "xFrame/Config/Game Config")]
    public class GameConfig : BaseConfig
    {
        [Header("Game Settings")]
        [Tooltip("游戏最大帧率")]
        public int TargetFrameRate = 60;
        
        [Tooltip("游戏音量")]
        [Range(0f, 1f)]
        public float MasterVolume = 1.0f;

        [Tooltip("是否开启调试模式")]
        public bool IsDebugMode = false;

        [Header("Player Settings")]
        [Tooltip("玩家初始生命值")]
        public int InitialHealth = 100;
        
        [Tooltip("玩家移动速度")]
        public float MoveSpeed = 5.0f;
    }
}
