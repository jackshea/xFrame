using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace xFrame.MVP.Examples
{
    /// <summary>
    /// 用户MVP使用示例
    /// </summary>
    public class UserMVPExample : MonoBehaviour
    {
        [Inject] private IMVPManager mvpManager;
        
        private UserMVPTriple userMVP;
        
        private async void Start()
        {
            userMVP = await mvpManager.CreateMVPAsync<UserMVPTriple, UserModel, UserView, UserPresenter>();
            
            await userMVP.ShowAsync();
        }
        
        private async void OnDestroy()
        {
            if (userMVP != null)
            {
                await mvpManager.DestroyMVPAsync(userMVP);
            }
        }
    }
}
