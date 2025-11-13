using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Core.Logging;
using xFrame.Core.MessagePipe;

namespace xFrame.Examples
{
    /// <summary>
    /// MessagePipe请求/响应模式示例
    /// 演示如何使用 IRequestHandler 和 IRequestAllHandler
    /// </summary>
    public class MessagePipeRequestResponseExample : MonoBehaviour, IStartable, IDisposable
    {
        /// <summary>
        /// 请求数据结构
        /// </summary>
        public struct DamageRequest
        {
            public int damage;
            public string targetId;
            public DamageType damageType;

            public enum DamageType
            {
                Physical,
                Magical,
                True
            }
        }

        /// <summary>
        /// 响应数据结构
        /// </summary>
        public struct DamageResponse
        {
            public int actualDamage;
            public bool isCritical;
            public bool isBlocked;
            public float remainingHealth;

            public override string ToString()
            {
                return $"实际伤害: {actualDamage}, 暴击: {isCritical}, 格挡: {isBlocked}, 剩余生命: {remainingHealth}";
            }
        }

        /// <summary>
        /// 伤害计算处理器
        /// </summary>
        public class DamageCalculationHandler : IRequestHandler<DamageRequest, DamageResponse>
        {
            private readonly IXLogger logger;

            public DamageCalculationHandler(IXLogger logger)
            {
                this.logger = logger;
            }

            public DamageResponse Invoke(DamageRequest request)
            {
                logger.Info($"[DamageCalculationHandler] 处理伤害请求: {request.damage} {request.damageType} -> {request.targetId}");

                var response = new DamageResponse
                {
                    actualDamage = request.damage,
                    isCritical = UnityEngine.Random.value < 0.2f, // 20%暴击率
                    isBlocked = UnityEngine.Random.value < 0.1f, // 10%格挡率
                    remainingHealth = UnityEngine.Random.Range(0f, 100f)
                };

                // 应用伤害类型修正
                switch (request.damageType)
                {
                    case DamageRequest.DamageType.Physical:
                        response.actualDamage = Mathf.RoundToInt(response.actualDamage * 0.9f);
                        break;
                    case DamageRequest.DamageType.Magical:
                        response.actualDamage = Mathf.RoundToInt(response.actualDamage * 1.1f);
                        break;
                    case DamageRequest.DamageType.True:
                        // 真实伤害不受修正
                        break;
                }

                if (response.isBlocked)
                {
                    response.actualDamage = Mathf.RoundToInt(response.actualDamage * 0.5f);
                }

                if (response.isCritical)
                {
                    response.actualDamage = Mathf.RoundToInt(response.actualDamage * 2f);
                }

                logger.Info($"[DamageCalculationHandler] 伤害计算完成: {response}");
                return response;
            }
        }

        /// <summary>
        /// 额外伤害加成处理器
        /// </summary>
        public class BonusDamageHandler : IRequestHandler<DamageRequest, DamageResponse>
        {
            private readonly IXLogger logger;

            public BonusDamageHandler(IXLogger logger)
            {
                this.logger = logger;
            }

            public DamageResponse Invoke(DamageRequest request)
            {
                logger.Info($"[BonusDamageHandler] 应用额外伤害加成");

                var response = new DamageResponse
                {
                    actualDamage = Mathf.RoundToInt(request.damage * 1.2f), // 20%额外伤害
                    isCritical = false,
                    isBlocked = false,
                    remainingHealth = 0f // 这个处理器只计算加成，不关心生命值
                };

                return response;
            }
        }

        /// <summary>
        /// 伤害日志记录处理器
        /// </summary>
        public class DamageLogHandler : IRequestHandler<DamageRequest, DamageResponse>
        {
            private readonly IXLogger logger;

            public DamageLogHandler(IXLogger logger)
            {
                this.logger = logger;
            }

            public DamageResponse Invoke(DamageRequest request)
            {
                logger.Info($"[DamageLogHandler] 记录伤害事件: 目标={request.targetId}, 伤害={request.damage}, 类型={request.damageType}");

                // 这个处理器只记录日志，返回空响应
                return new DamageResponse
                {
                    actualDamage = 0,
                    isCritical = false,
                    isBlocked = false,
                    remainingHealth = 0f
                };
            }
        }

        // 依赖注入的服务
        [Inject] private IRequestHandler<DamageRequest, DamageResponse> damageHandler;
        [Inject] private IRequestAllHandler<DamageRequest, DamageResponse> allDamageHandlers;
        [Inject] private IXLogger logger;

        public void Start()
        {
            logger.Info("[MessagePipeRequestResponseExample] 请求/响应示例初始化完成");
        }

        /// <summary>
        /// 测试单个处理器
        /// </summary>
        [ContextMenu("测试单个伤害处理器")]
        public void TestSingleDamageHandler()
        {
            var request = new DamageRequest
            {
                damage = 100,
                targetId = "Monster_001",
                damageType = DamageRequest.DamageType.Physical
            };

            logger.Info("[MessagePipeRequestResponseExample] === 测试单个伤害处理器 ===");
            
            var response = damageHandler.Invoke(request);
            logger.Info($"[MessagePipeRequestResponseExample] 处理结果: {response}");
        }

        /// <summary>
        /// 测试所有处理器
        /// </summary>
        [ContextMenu("测试所有伤害处理器")]
        public void TestAllDamageHandlers()
        {
            var request = new DamageRequest
            {
                damage = 150,
                targetId = "Boss_001",
                damageType = DamageRequest.DamageType.Magical
            };

            logger.Info("[MessagePipeRequestResponseExample] === 测试所有伤害处理器 ===");
            
            var responses = allDamageHandlers.InvokeAll(request);
            logger.Info($"[MessagePipeRequestResponseExample] 收到 {responses.Length} 个响应:");
            
            for (int i = 0; i < responses.Length; i++)
            {
                logger.Info($"[MessagePipeRequestResponseExample] 响应 {i + 1}: {responses[i]}");
            }
        }

        /// <summary>
        /// 批量测试不同类型的伤害
        /// </summary>
        [ContextMenu("批量测试伤害类型")]
        public void TestBatchDamageTypes()
        {
            logger.Info("[MessagePipeRequestResponseExample] === 批量测试伤害类型 ===");

            var damageTypes = new[]
            {
                DamageRequest.DamageType.Physical,
                DamageRequest.DamageType.Magical,
                DamageRequest.DamageType.True
            };

            foreach (var damageType in damageTypes)
            {
                var request = new DamageRequest
                {
                    damage = UnityEngine.Random.Range(50, 200),
                    targetId = $"Target_{damageType}",
                    damageType = damageType
                };

                var response = damageHandler.Invoke(request);
                logger.Info($"[MessagePipeRequestResponseExample] {damageType} 伤害测试: {response}");
            }
        }

        /// <summary>
        /// 在Inspector中显示控制界面
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 220, 300, 150));
            GUILayout.Label("请求/响应示例控制", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("测试单个伤害处理器"))
            {
                TestSingleDamageHandler();
            }
            
            if (GUILayout.Button("测试所有伤害处理器"))
            {
                TestAllDamageHandlers();
            }
            
            if (GUILayout.Button("批量测试伤害类型"))
            {
                TestBatchDamageTypes();
            }
            
            GUILayout.EndArea();
        }

        public void Dispose()
        {
            logger.Info("[MessagePipeRequestResponseExample] 清理请求/响应示例资源");
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
