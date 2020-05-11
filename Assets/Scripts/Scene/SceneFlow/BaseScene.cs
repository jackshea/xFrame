namespace Scene.SceneFlow
{
    public class BaseScene : xFrame.Infrastructure.Scene
    {
        public override string DefaultKernelScene
        {
            get
            {
                return "KernelScene";
            }
        }
    }
}