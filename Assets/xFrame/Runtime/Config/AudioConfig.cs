using UnityEngine;

namespace xFrame.Config
{
    /// <summary>
    /// 音频配置，用于管理游戏中的音频相关设置。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioConfig", menuName = "xFrame/Config/Audio Config")]
    public class AudioConfig : BaseConfig
    {
        [Header("音量设置 / Volume Settings")]
        [Tooltip("主音量")]
        [Range(0f, 1f)]
        public float MasterVolume = 1.0f;

        [Tooltip("背景音乐音量")]
        [Range(0f, 1f)]
        public float MusicVolume = 0.8f;

        [Tooltip("音效音量")]
        [Range(0f, 1f)]
        public float SFXVolume = 1.0f;

        [Tooltip("语音音量")]
        [Range(0f, 1f)]
        public float VoiceVolume = 1.0f;

        [Header("音频行为 / Audio Behavior")]
        [Tooltip("是否静音")]
        public bool IsMuted = false;

        [Tooltip("失去焦点时是否暂停音频")]
        public bool PauseOnFocusLost = true;

        [Tooltip("音频淡入淡出时间（秒）")]
        [Range(0f, 3f)]
        public float FadeDuration = 0.5f;
    }
}
