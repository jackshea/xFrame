using System;
using System.Collections.Generic;
using VContainer;
using xFrame.Runtime.Logging;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP三元组对象池实现
    /// </summary>
    public class MVPTriplePool : IMVPTriplePool
    {
        private readonly IObjectResolver _container;
        private readonly IXLogger _logger;
        private readonly Dictionary<Type, Queue<IMVPTriple>> _pools;
        
        public MVPTriplePool(IObjectResolver container, IXLogger logger)
        {
            _container = container;
            _logger = logger;
            _pools = new Dictionary<Type, Queue<IMVPTriple>>();
        }
        
        public TMVPTriple Get<TMVPTriple>() where TMVPTriple : class, IMVPTriple
        {
            var type = typeof(TMVPTriple);
            
            if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var mvp = pool.Dequeue() as TMVPTriple;
                _logger.Info($"MVP retrieved from pool: {type.Name}");
                return mvp;
            }
            
            _logger.Info($"MVP pool empty, creating new instance: {type.Name}");
            return null;
        }
        
        public void Return<TMVPTriple>(TMVPTriple mvpTriple) where TMVPTriple : class, IMVPTriple
        {
            if (mvpTriple == null)
            {
                _logger.Warning("Attempted to return null MVP to pool");
                return;
            }
            
            var type = typeof(TMVPTriple);
            
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<IMVPTriple>();
                _pools[type] = pool;
            }
            
            pool.Enqueue(mvpTriple);
            _logger.Info($"MVP returned to pool: {type.Name}");
        }
    }
}
