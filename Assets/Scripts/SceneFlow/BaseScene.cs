using Framework.Runtime.Infrastructure;

namespace SceneFlow
{
    public class BaseScene : Scene
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