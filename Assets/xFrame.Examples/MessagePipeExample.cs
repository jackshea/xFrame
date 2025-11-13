using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace xFrame.Examples
{
    /// <summary>
    /// MessagePipe使用示例
    /// 演示如何在xFrame框架中使用MessagePipe进行事件通信
    /// </summary>
    public class MessagePipeExample : MonoBehaviour, IStartable, IDisposable
    {
        [Header("示例控制")]
        [SerializeField]
        private bool enableAutoTest = true;

        [SerializeField]
        private float testInterval = 2f;

        private readonly List<IDisposable> subscriptions = new();

        [Inject]
        private IAsyncPublisher<GameEvent> asyncGameEventPublisher;

        [Inject]
        private IAsyncSubscriber<GameEvent> asyncGameEventSubscriber;

        // 订阅管理
        private IDisposable bag;

        // 依赖注入的服务
        [Inject]
        private IPublisher<GameEvent> gameEventPublisher;

        [Inject]
        private ISubscriber<GameEvent> gameEventSubscriber;

        [Inject]
        private IPublisher<string, HealthChangedEvent> healthEventPublisher;

        [Inject]
        private ISubscriber<string, HealthChangedEvent> healthEventSubscriber;

        private float nextTestTime;

        /// <summary>
        /// Update方法中执行自动测试
        /// </summary>
        private void Update()
        {
            if (enableAutoTest && Time.time >= nextTestTime)
            {
                RunEventTest();
                nextTestTime = Time.time + testInterval;
            }
        }

        /// <summary>
        /// OnDestroy时清理资源
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// 在Inspector中显示当前状态
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("MessagePipe示例控制",
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(10);

            if (GUILayout.Button("运行事件测试")) RunEventTest();

            if (GUILayout.Button("测试请求/响应")) TestRequestResponse();

            GUILayout.Space(10);
            GUILayout.Label($"自动测试: {(enableAutoTest ? "开启" : "关闭")}");
            GUILayout.Label($"测试间隔: {testInterval}秒");

            GUILayout.EndArea();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[MessagePipeExample] 清理MessagePipe示例资源");

            bag?.Dispose();
            bag = null;

            foreach (var subscription in subscriptions) subscription?.Dispose();
            subscriptions.Clear();
        }

        /// <summary>
        /// VContainer初始化后调用
        /// </summary>
        public void Start()
        {
            Debug.Log("[MessagePipeExample] 开始初始化MessagePipe示例");

            SetupEventSubscriptions();

            if (enableAutoTest) nextTestTime = Time.time + testInterval;
        }

        /// <summary>
        /// 设置事件订阅
        /// </summary>
        private void SetupEventSubscriptions()
        {
            var bagBuilder = DisposableBag.CreateBuilder();

            // 订阅普通游戏事件
            gameEventSubscriber.Subscribe(OnGameEvent).AddTo(bagBuilder);

            // 订阅带过滤器的游戏事件（只处理特定事件）
            gameEventSubscriber.Subscribe(OnSpecialGameEvent,
                x => x.eventName.Contains("Special")).AddTo(bagBuilder);

            // 订阅异步游戏事件
            asyncGameEventSubscriber.Subscribe(OnAsyncGameEvent).AddTo(bagBuilder);

            // 订阅特定玩家的生命值事件
            healthEventSubscriber.Subscribe("Player1", OnPlayerHealthChanged).AddTo(bagBuilder);
            healthEventSubscriber.Subscribe("Player2", OnPlayerHealthChanged).AddTo(bagBuilder);

            // 构建订阅包
            bag = bagBuilder.Build();

            Debug.Log("[MessagePipeExample] 事件订阅设置完成");
        }

        /// <summary>
        /// 处理普通游戏事件
        /// </summary>
        /// <param name="gameEvent">游戏事件数据</param>
        private void OnGameEvent(GameEvent gameEvent)
        {
            Debug.Log($"[MessagePipeExample] 收到游戏事件: {gameEvent}");
        }

        /// <summary>
        /// 处理特殊游戏事件
        /// </summary>
        /// <param name="gameEvent">特殊游戏事件数据</param>
        private void OnSpecialGameEvent(GameEvent gameEvent)
        {
            Debug.Log($"[MessagePipeExample] 收到特殊游戏事件: {gameEvent}");
        }

        /// <summary>
        /// 处理异步游戏事件
        /// </summary>
        /// <param name="gameEvent">异步游戏事件数据</param>
        private async UniTask OnAsyncGameEvent(GameEvent gameEvent, CancellationToken cancellationToken)
        {
            Debug.Log($"[MessagePipeExample] 开始处理异步游戏事件: {gameEvent}");

            // 模拟异步处理
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);

            Debug.Log($"[MessagePipeExample] 异步游戏事件处理完成: {gameEvent}");
        }

        /// <summary>
        /// 处理玩家生命值变化事件
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="healthEvent">生命值事件数据</param>
        private void OnPlayerHealthChanged(HealthChangedEvent healthEvent)
        {
            Debug.Log($"[MessagePipeExample] 玩家生命值变化: {healthEvent}");
        }

        /// <summary>
        /// 运行事件测试
        /// </summary>
        [ContextMenu("运行事件测试")]
        public void RunEventTest()
        {
            Debug.Log("[MessagePipeExample] === 开始事件测试 ===");

            // 发布普通游戏事件
            var normalEvent = new GameEvent
            {
                eventName = "NormalEvent",
                value = Random.Range(1, 100),
                timestamp = DateTime.Now
            };
            gameEventPublisher.Publish(normalEvent);

            // 发布特殊游戏事件
            var specialEvent = new GameEvent
            {
                eventName = "SpecialEvent_Bonus",
                value = Random.Range(100, 200),
                timestamp = DateTime.Now
            };
            gameEventPublisher.Publish(specialEvent);

            // 发布异步游戏事件
            var asyncEvent = new GameEvent
            {
                eventName = "AsyncEvent",
                value = Random.Range(200, 300),
                timestamp = DateTime.Now
            };
            asyncGameEventPublisher.Publish(asyncEvent);

            // 发布玩家生命值事件
            var healthEvent = new HealthChangedEvent
            {
                currentHealth = Random.Range(50, 100),
                maxHealth = 100,
                healthPercentage = Random.Range(0.5f, 1f)
            };

            healthEventPublisher.Publish("Player1", healthEvent);

            healthEvent = new HealthChangedEvent
            {
                currentHealth = Random.Range(30, 80),
                maxHealth = 100,
                healthPercentage = Random.Range(0.3f, 0.8f)
            };
            healthEventPublisher.Publish("Player2", healthEvent);

            Debug.Log("[MessagePipeExample] === 事件测试完成 ===");
        }

        /// <summary>
        /// 测试请求/响应模式
        /// </summary>
        [ContextMenu("测试请求/响应")]
        public void TestRequestResponse()
        {
            Debug.Log("[MessagePipeExample] === 测试请求/响应模式 ===");

            // 这里需要实现IRequestHandler来演示
            // 由于篇幅限制，这里只是示例代码结构
            Debug.Log("[MessagePipeExample] 请求/响应模式需要额外的Handler实现");
        }

        /// <summary>
        /// 游戏事件数据结构
        /// </summary>
        [Serializable]
        public struct GameEvent
        {
            public string eventName;
            public int value;
            public DateTime timestamp;

            public override string ToString()
            {
                return $"GameEvent: {eventName}, Value: {value}, Time: {timestamp:HH:mm:ss}";
            }
        }

        /// <summary>
        /// 玩家生命值变化事件
        /// </summary>
        [Serializable]
        public struct HealthChangedEvent
        {
            public int currentHealth;
            public int maxHealth;
            public float healthPercentage;

            public override string ToString()
            {
                return $"Health: {currentHealth}/{maxHealth} ({healthPercentage:P1})";
            }
        }
    }
}