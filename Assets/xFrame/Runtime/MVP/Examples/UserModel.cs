using Cysharp.Threading.Tasks;

namespace xFrame.MVP.Examples
{
    /// <summary>
    /// 用户数据Model示例
    /// </summary>
    public interface IUserModel : IModel
    {
        string UserName { get; set; }
        int Level { get; set; }
        int Experience { get; set; }
    }
    
    /// <summary>
    /// 用户数据Model实现
    /// </summary>
    public class UserModel : BaseModel, IUserModel
    {
        public string UserName { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadUserDataAsync();
        }
        
        private async UniTask LoadUserDataAsync()
        {
            UserName = "Player";
            Level = 1;
            Experience = 0;
            
            NotifyDataChanged();
            await UniTask.CompletedTask;
        }
    }
}
