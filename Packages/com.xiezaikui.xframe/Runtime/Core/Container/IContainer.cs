namespace xFrame.Core
{
    public interface IContainer
    {
        void Register(string name, object target);

        void Register<T>(T target);

        void Unregister(string name);

        void Unregister<T>();

        object Resolve(string name);

        T Resolve<T>();
    }
}
