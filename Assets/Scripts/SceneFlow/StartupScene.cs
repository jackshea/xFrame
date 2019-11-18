using NLog;

namespace SceneFlow
{
    public class StartupScene : BaseScene
    {
        static readonly Logger _log = LoggerFactory.GetLogger(typeof(StartupScene).Name);
        public override void KernelLoaded()
        {
            base.KernelLoaded();
            _log.Trace("test nlog trace");
            _log.Debug("test nlog debug");
            _log.Info("test nlog info");
            _log.Warn("test nlog warning");
            _log.Error("test nlog error");
            _log.Fatal("test nlog fatal");
        }
    }
}
