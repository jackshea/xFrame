using NLog;
using UnityEngine;
using Logger = NLog.Logger;

namespace SceneFlow
{
    public class StartupScene : BaseScene
    {
        public override void KernelLoaded()
        {
            base.KernelLoaded();
            Logger _log = LogManager.GetCurrentClassLogger();
            Debug.Log("KernelLoaded");
            _log.Trace("test nlog trace");
            _log.Debug("test nlog debug");
            _log.Info("test nlog info");
            _log.Warn("test nlog warning");
            _log.Error("test nlog error");
            _log.Fatal("test nlog fatal");
        }
    }
}
