using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace xFrame.Runtime.StateMachine
{
    /// <summary>
    /// 状态机管理服务，负责管理多个状态机实例，实现VContainer的ITickable接口自动更新
    /// </summary>
    public class StateMachineServiceService : IStateMachineService, ITickable, IDisposable
    {
        private readonly Dictionary<string, object> _stateMachines = new Dictionary<string, object>();
        private readonly List<object> _updateableStateMachines = new List<object>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public StateMachineServiceService()
        {
            Debug.Log("[StateMachineModuleService] Created");
        }

        /// <summary>
        /// VContainer的Tick回调，每帧自动调用
        /// </summary>
        void ITickable.Tick()
        {
            Update();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
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
            Debug.Log("[StateMachineModuleService] Disposed");
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
            stateMachine.Name = name;
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
            stateMachine.Name = name;
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
        /// 更新所有自动更新的状态机（内部方法）
        /// </summary>
        private void Update()
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
