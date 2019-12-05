namespace Scene.SceneFlow
{
    public class BaseScene : Framework.Runtime.Infrastructure.Scene
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