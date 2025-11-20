using System;
using System.Collections.Generic;
using xFrame.Bootstrapper;
using UnityEngine;

namespace xFrame.StateMachine
{
    /// <summary>
    /// 状态机管理模块，负责管理多个状态机实例
    /// </summary>
    public class StateMachineModule : IModule
    {
        private readonly Dictionary<string, object> _stateMachines = new Dictionary<string, object>();
        private readonly List<object> _updateableStateMachines = new List<object>();

        /// <summary>
        /// 模块名称
        /// </summary>
        public string Name => "StateMachineModule";

        /// <summary>
        /// 模块优先级
        /// </summary>
        public int Priority => 50;

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[StateMachineModule] Initialized");
        }

        /// <summary>
        /// 关闭模块
        /// </summary>
        public void Shutdown()
        {
            // 停止所有状态机
            foreach (var sm in _updateableStateMachines)
            {
                if (sm is StateMachine stateMachine)
                {
                    stateMachine.Stop();
                }
                else
                {
                    var stopMethod = sm.GetType().GetMethod("Stop");
                    stopMethod?.Invoke(sm, null);
                }
            }

            _stateMachines.Clear();
            _updateableStateMachines.Clear();
            Debug.Log("[StateMachineModule] Shutdown");
        }

        /// <summary>
        /// 创建状态机（不带上下文）
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="autoUpdate">是否自动更新</param>
        /// <returns>状态机实例</returns>
        public StateMachine CreateStateMachine(string name, bool autoUpdate = true)
        {
            if (_stateMachines.ContainsKey(name))
            {
                throw new InvalidOperationException($"StateMachine with name '{name}' already exists.");
            }

            var stateMachine = new StateMachine();
            _stateMachines[name] = stateMachine;

            if (autoUpdate)
            {
                _updateableStateMachines.Add(stateMachine);
            }

            return stateMachine;
        }

        /// <summary>
        /// 创建状态机（带上下文）
        /// </summary>
        /// <typeparam name="TContext">上下文类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <param name="context">上下文实例</param>
        /// <param name="autoUpdate">是否自动更新</param>
        /// <returns>状态机实例</returns>
        public StateMachine<TContext> CreateStateMachine<TContext>(string name, TContext context, bool autoUpdate = true)
        {
            if (_stateMachines.ContainsKey(name))
            {
                throw new InvalidOperationException($"StateMachine with name '{name}' already exists.");
            }

            var stateMachine = new StateMachine<TContext>(context);
            _stateMachines[name] = stateMachine;

            if (autoUpdate)
            {
                _updateableStateMachines.Add(stateMachine);
            }

            return stateMachine;
        }

        /// <summary>
        /// 获取状态机（不带上下文）
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机实例</returns>
        public StateMachine GetStateMachine(string name)
        {
            if (_stateMachines.TryGetValue(name, out var stateMachine))
            {
                return stateMachine as StateMachine;
            }
            return null;
        }

        /// <summary>
        /// 获取状态机（带上下文）
        /// </summary>
        /// <typeparam name="TContext">上下文类型</typeparam>
        /// <param name="name">状态机名称</param>
        /// <returns>状态机实例</returns>
        public StateMachine<TContext> GetStateMachine<TContext>(string name)
        {
            if (_stateMachines.TryGetValue(name, out var stateMachine))
            {
                return stateMachine as StateMachine<TContext>;
            }
            return null;
        }

        /// <summary>
        /// 移除状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        public void RemoveStateMachine(string name)
        {
            if (_stateMachines.TryGetValue(name, out var stateMachine))
            {
                // 停止状态机
                if (stateMachine is StateMachine sm)
                {
                    sm.Stop();
                }
                else
                {
                    var stopMethod = stateMachine.GetType().GetMethod("Stop");
                    stopMethod?.Invoke(stateMachine, null);
                }

                _updateableStateMachines.Remove(stateMachine);
                _stateMachines.Remove(name);
            }
        }

        /// <summary>
        /// 更新所有自动更新的状态机
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < _updateableStateMachines.Count; i++)
            {
                var sm = _updateableStateMachines[i];
                if (sm is StateMachine stateMachine)
                {
                    stateMachine.Update();
                }
                else
                {
                    var updateMethod = sm.GetType().GetMethod("Update");
                    updateMethod?.Invoke(sm, null);
                }
            }
        }
    }
}
