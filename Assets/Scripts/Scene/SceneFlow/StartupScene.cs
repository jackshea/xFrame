using UnityEngine;
using xFrame.Core;
using xFrame.Infrastructure;

namespace Scene.SceneFlow
{
    public class StartupScene : BaseScene
    {
        [Inject] public ILog log;
        private ILog otherLog;
        public override void KernelLoaded()
        {
            base.KernelLoaded();
            log = uFrameKernel.Container.Resolve<ILog>();
            Debug.Log("KernelLoaded");
            log.Trace("test nlog trace");
            log.Debug("test nlog debug");
            log.Info("test nlog info");
            log.Warn("test nlog warning");
            log.Error("test nlog error");
            log.Fatal("test nlog fatal");
            log.Assert(false, "test assert");

            otherLog = XFrameContext.GetLogger();
            otherLog.Info("This is xFrame Logger!");
        }
    }
}
