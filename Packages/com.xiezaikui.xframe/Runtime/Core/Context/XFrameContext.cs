using xFrame.Logger;

namespace xFrame.Core
{
    public class XFrameContext : Context
    {
        private static ILog _logger;

        public static void SetLogger(ILog logger)
        {
            _logger = logger;
        }

        public static ILog GetLogger()
        {
            return _logger;
        }
    }
}