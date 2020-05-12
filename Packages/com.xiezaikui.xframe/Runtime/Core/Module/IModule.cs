namespace Core.Module
{
    public interface IModule
    {
        void PreInitialize();
        void Initialize();
        void Release();
        void AfterRelease();
    }
}