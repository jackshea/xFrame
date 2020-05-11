using NLog;
using xFrame.Logger;

namespace xFrame.Logger
{
    public class LogImpl : ILog
    {
        NLog.Logger log = LogManager.GetLogger("Default");
        public void Assert(bool condition, string msg)
        {
            if (!condition)
            {
                log.Info(msg);
            }
        }

        public void Trace(string msg)
        {
            log.Trace(msg);
        }

        public void Debug(string msg)
        {
            log.Debug(msg);
        }

        public void Info(string msg)
        {
            log.Info(msg);
        }

        public void Warn(string msg)
        {
            log.Warn(msg);
        }

        public void Error(string msg)
        {
            log.Error(msg);
        }

        public void Fatal(string msg)
        {
            log.Fatal(msg);
        }
    }
}