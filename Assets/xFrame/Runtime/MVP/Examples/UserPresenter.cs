using Cysharp.Threading.Tasks;

namespace xFrame.MVP.Examples
{
    /// <summary>
    /// 用户界面Presenter实现
    /// </summary>
    public class UserPresenter : BasePresenter
    {
        private IUserModel userModel;
        private IUserView userView;
        
        protected override async UniTask OnBindAsync()
        {
            userModel = Model as IUserModel;
            userView = View as IUserView;
            
            if (userView != null)
            {
                userView.OnLevelUpButtonClicked += OnLevelUpButtonClicked;
            }
            
            UpdateView();
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnUnbindAsync()
        {
            if (userView != null)
            {
                userView.OnLevelUpButtonClicked -= OnLevelUpButtonClicked;
            }
            await UniTask.CompletedTask;
        }
        
        protected override void OnModelDataChanged(IModel model)
        {
            UpdateView();
        }
        
        private void UpdateView()
        {
            if (userModel != null && userView != null)
            {
                userView.UpdateUserInfo(
                    userModel.UserName, 
                    userModel.Level, 
                    userModel.Experience);
            }
        }
        
        private void OnLevelUpButtonClicked()
        {
            if (userModel != null)
            {
                userModel.Level++;
                userModel.Experience = 0;
                
                Logger.Info($"User leveled up to {userModel.Level}");
            }
        }
        
        protected override async UniTask OnShowAsync()
        {
            Logger.Info("User panel shown");
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnHideAsync()
        {
            Logger.Info("User panel hidden");
            await UniTask.CompletedTask;
        }
    }
}
