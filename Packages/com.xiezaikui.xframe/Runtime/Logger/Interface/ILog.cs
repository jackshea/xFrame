namespace xFrame.Logger
{
    public interface ILog
    {
        void Assert(bool condition, string msg);
        void Trace(string msg);
        void Debug(string msg);
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
        void Fatal(string msg);
    }
}