using xFrame.Infrastructure;
using xFrame.Logger;
using UnityEngine;

namespace Scene.SceneFlow
{
    public class StartupScene : BaseScene
    {
        [Inject] public ILog log;
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
        }
    }
}
