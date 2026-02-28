using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using xFrame.Runtime.Scheduler;
using VContainer;

namespace xFrame.Examples
{
    /// <summary>
    /// 调度器模块使用示例
    /// 演示如何使用调度器系统来管理延迟执行、定时重复执行、下一帧执行和异步方法调度
    /// </summary>
    public class SchedulerModuleUsageExample : MonoBehaviour
    {
        private ISchedulerService _scheduler;

        [SerializeField]
        private bool _showGUI = true;

        private int _counter1 = 0;
        private int _counter2 = 0;
        private int _counter3 = 0;
        private int _intervalTaskId = 0;
        private bool _isIntervalPaused = false;

        /// <summary>
        /// 构造函数注入
        /// </summary>
        public SchedulerModuleUsageExample(ISchedulerService scheduler)
        {
            _scheduler = scheduler;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Start()
        {
            Debug.Log("=== 调度器系统示例开始 ===");

            // 示例1: 延迟执行
            DelayExecutionExample();

            // 示例2: 定时重复执行
            IntervalExecutionExample();

            // 示例3: 下一帧执行
            NextFrameExecutionExample();

            // 示例4: 异步方法调度
            AsyncMethodExample();

            // 示例5: 任务暂停和恢复
            PauseResumeExample();

            // 示例6: 任务取消
            CancelExample();

            Debug.Log("调度器系统示例初始化完成");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            _scheduler?.CancelAll();
            Debug.Log("调度器系统示例结束");
        }

        /// <summary>
        /// 显示GUI
        /// </summary>
        private void OnGUI()
        {
            if (!_showGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 320, 300));
            GUILayout.Label("=== 调度器系统示例 ===");

            GUILayout.Label($"延迟计数器: {_counter1}");
            GUILayout.Label($"间隔计数器: {_counter2}");
            GUILayout.Label($"下一帧标记: {_counter3}");
            GUILayout.Label($"活动任务数: {_scheduler.ActiveTaskCount}");

            GUILayout.Space(10);

            if (GUILayout.Button("暂停/恢复间隔任务"))
            {
                ToggleIntervalTask();
            }

            if (GUILayout.Button("取消所有任务"))
            {
                _scheduler.CancelAll();
                Debug.Log("已取消所有任务");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("创建新延迟任务 (1秒)"))
            {
                _scheduler.Delay(1f, () =>
                {
                    Debug.Log("手动创建的延迟任务执行");
                    _counter1++;
                });
            }

            if (GUILayout.Button("创建新间隔任务 (0.5秒, 5次)"))
            {
                _scheduler.Interval(0.5f, () =>
                {
                    Debug.Log("手动创建的间隔任务执行");
                    _counter2++;
                }, 5);
            }

            if (GUILayout.Button("创建下一帧任务"))
            {
                _scheduler.NextFrame(() =>
                {
                    Debug.Log("下一帧任务执行");
                    _counter3++;
                });
            }

            GUILayout.Space(10);

            if (GUILayout.Button("显示所有示例"))
            {
                DemonstrateAllFeatures();
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 示例1: 延迟执行
        /// </summary>
        private void DelayExecutionExample()
        {
            Debug.Log("--- 示例1: 延迟执行 ---");

            // 基础延迟执行：1秒后执行
            _scheduler.Delay(1f, () =>
            {
                Debug.Log("1秒后执行的任务");
                _counter1++;
            });

            // 不受Time.timeScale影响的延迟执行
            var taskId = _scheduler.Delay(2f, () =>
            {
                Debug.Log("2秒后执行的任务（不受Time.timeScale影响）");
            }, useTimeScale: false);

            // 多个延迟任务
            for (int i = 0; i < 3; i++)
            {
                float delay = (i + 1) * 0.5f;
                _scheduler.Delay(delay, () =>
                {
                    Debug.Log($"延迟 {delay} 秒后的任务执行");
                });
            }
        }

        /// <summary>
        /// 示例2: 定时重复执行
        /// </summary>
        private void IntervalExecutionExample()
        {
            Debug.Log("--- 示例2: 定时重复执行 ---");

            // 固定次数重复执行：每0.5秒执行一次，共5次
            _intervalTaskId = _scheduler.Interval(0.5f, () =>
            {
                Debug.Log("间隔任务执行");
                _counter2++;
            }, 5);

            // 无限重复执行：每1秒执行一次
            var infiniteTaskId = _scheduler.Interval(1f, () =>
            {
                Debug.Log("无限间隔任务执行");
            }, -1);

            // 不受Time.timeScale影响的间隔任务
            _scheduler.Interval(0.3f, () =>
            {
                Debug.Log("不受timeScale影响的间隔任务");
            }, 3, useTimeScale: false);
        }

        /// <summary>
        /// 示例3: 下一帧执行
        /// </summary>
        private void NextFrameExecutionExample()
        {
            Debug.Log("--- 示例3: 下一帧执行 ---");

            // 下一帧执行一次
            _scheduler.NextFrame(() =>
            {
                Debug.Log("下一帧执行的任务");
                _counter3++;
            });

            // 使用NextFrame来确保某些操作在下一帧执行
            _scheduler.NextFrame(() =>
            {
                Debug.Log("确保在下一帧执行某些操作");
            });
        }

        /// <summary>
        /// 示例4: 异步方法调度
        /// </summary>
        private void AsyncMethodExample()
        {
            Debug.Log("--- 示例4: 异步方法调度 ---");

            // 调度一个简单的异步方法
            _scheduler.ScheduleAsync(async (ct) =>
            {
                Debug.Log("异步任务开始");
                await UniTask.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken: ct);
                Debug.Log("异步任务完成（等待1秒后）");
            });

            // 调度带取消令牌的异步方法
            var cts = new System.Threading.CancellationTokenSource();
            _scheduler.ScheduleAsync(async (ct) =>
            {
                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Debug.Log($"异步循环 {i + 1}/3");
                        await UniTask.Delay(TimeSpan.FromMilliseconds(500), cancellationToken: ct);
                    }
                }
                catch (System.OperationCanceledException)
                {
                    Debug.Log("异步任务被取消");
                }
            }, cts.Token);

            // 演示链式异步操作
            _scheduler.ScheduleAsync(async (ct) =>
            {
                Debug.Log("链式异步操作开始");
                await UniTask.Delay(TimeSpan.FromMilliseconds(300), cancellationToken: ct);
                Debug.Log("第一步完成");
                await UniTask.Delay(TimeSpan.FromMilliseconds(300), cancellationToken: ct);
                Debug.Log("第二步完成");
                await UniTask.Delay(TimeSpan.FromMilliseconds(300), cancellationToken: ct);
                Debug.Log("第三步完成");
            });
        }

        /// <summary>
        /// 示例5: 任务暂停和恢复
        /// </summary>
        private void PauseResumeExample()
        {
            Debug.Log("--- 示例5: 任务暂停和恢复 ---");

            // 创建一个可以暂停恢复的任务
            var taskId = _scheduler.Interval(0.3f, () =>
            {
                Debug.Log("可暂停的任务正在执行");
            }, 10);

            // 延迟1秒后暂停任务
            _scheduler.Delay(1f, () =>
            {
                Debug.Log("暂停任务");
                _scheduler.Pause(taskId);
            });

            // 再延迟1秒后恢复任务
            _scheduler.Delay(2f, () =>
            {
                Debug.Log("恢复任务");
                _scheduler.Resume(taskId);
            });
        }

        /// <summary>
        /// 示例6: 任务取消
        /// </summary>
        private void CancelExample()
        {
            Debug.Log("--- 示例6: 任务取消 ---");

            // 创建一个将被取消的任务
            var taskId = _scheduler.Delay(0.5f, () =>
            {
                Debug.Log("这个任务不应该被执行（被取消）");
            });

            // 立即取消任务
            _scheduler.Cancel(taskId);
            Debug.Log($"任务已取消，任务ID: {taskId}");

            // 创建多个延迟任务，然后取消所有
            for (int i = 0; i < 5; i++)
            {
                _scheduler.Delay(1f + i * 0.1f, () =>
                {
                    Debug.Log($"延迟任务 {i} 不应该被执行");
                });
            }

            // 0.3秒后取消所有任务
            _scheduler.Delay(0.3f, () =>
            {
                Debug.Log("取消所有任务");
                _scheduler.CancelAll();
            });
        }

        /// <summary>
        /// 切换间隔任务的暂停状态
        /// </summary>
        private void ToggleIntervalTask()
        {
            if (_intervalTaskId == 0) return;

            if (_isIntervalPaused)
            {
                _scheduler.Resume(_intervalTaskId);
                _isIntervalPaused = false;
                Debug.Log("间隔任务已恢复");
            }
            else
            {
                _scheduler.Pause(_intervalTaskId);
                _isIntervalPaused = true;
                Debug.Log("间隔任务已暂停");
            }
        }

        /// <summary>
        /// 演示所有功能
        /// </summary>
        private void DemonstrateAllFeatures()
        {
            Debug.Log("=== 演示所有功能 ===");

            // 清除现有任务
            _scheduler.CancelAll();
            _counter1 = 0;
            _counter2 = 0;
            _counter3 = 0;

            // 重新运行所有示例
            DelayExecutionExample();
            IntervalExecutionExample();
            NextFrameExecutionExample();
            AsyncMethodExample();
            PauseResumeExample();
            CancelExample();

            Debug.Log("所有功能演示已开始");
        }

        /// <summary>
        /// 高级用法：游戏逻辑中的实际应用
        /// </summary>
        public void AdvancedGameLogicExamples()
        {
            // 示例：技能冷却
            const float cooldownTime = 5f;
            bool isSkillOnCooldown = false;
            var skillTaskId = 0;

            // 使用技能
            void UseSkill()
            {
                if (isSkillOnCooldown)
                {
                    Debug.Log("技能冷却中");
                    return;
                }

                isSkillOnCooldown = true;
                Debug.Log("技能已使用");

                // 设置冷却时间
                skillTaskId = _scheduler.Delay(cooldownTime, () =>
                {
                    isSkillOnCooldown = false;
                    Debug.Log("技能冷却完成");
                });
            }

            // 示例：定时刷新
            _scheduler.Interval(60f, () =>
            {
                Debug.Log("每60秒刷新一次数据");
                // 刷新游戏数据、排行榜等
            });

            // 示例：动画序列
            void PlayAnimationSequence()
            {
                _scheduler.Delay(0.2f, () => Debug.Log("动画阶段1"));
                _scheduler.Delay(0.4f, () => Debug.Log("动画阶段2"));
                _scheduler.Delay(0.6f, () => Debug.Log("动画阶段3"));
                _scheduler.Delay(0.8f, () => Debug.Log("动画结束"));
            }

            // 示例：游戏定时器
            float gameTime = 0f;
            _scheduler.Interval(1f, () =>
            {
                gameTime++;
                Debug.Log($"游戏时间: {gameTime}秒");
            }, -1, useTimeScale: false);

            // 示例：敌人生成
            _scheduler.Interval(2f, () =>
            {
                Debug.Log("生成敌人");
                // 生成敌人逻辑
            }, 10);
        }
    }
}
