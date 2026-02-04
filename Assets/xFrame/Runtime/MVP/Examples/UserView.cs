using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace xFrame.MVP.Examples
{
    /// <summary>
    /// 用户界面View接口
    /// </summary>
    public interface IUserView : IView
    {
        void UpdateUserInfo(string userName, int level, int experience);
        event Action OnLevelUpButtonClicked;
    }
    
    /// <summary>
    /// 用户界面View实现
    /// </summary>
    public class UserView : BaseView, IUserView
    {
        [SerializeField] private Text userNameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text experienceText;
        [SerializeField] private Button levelUpButton;
        
        public event Action OnLevelUpButtonClicked;
        
        protected override void OnPresenterBound()
        {
            if (levelUpButton != null)
            {
                levelUpButton.onClick.AddListener(() => OnLevelUpButtonClicked?.Invoke());
            }
        }
        
        protected override void OnPresenterUnbound()
        {
            if (levelUpButton != null)
            {
                levelUpButton.onClick.RemoveAllListeners();
            }
            OnLevelUpButtonClicked = null;
        }
        
        public void UpdateUserInfo(string userName, int level, int experience)
        {
            if (userNameText != null)
                userNameText.text = userName;
            
            if (levelText != null)
                levelText.text = $"Level: {level}";
            
            if (experienceText != null)
                experienceText.text = $"EXP: {experience}";
        }
        
        protected override async UniTask OnShowAsync()
        {
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnHideAsync()
        {
            await UniTask.CompletedTask;
        }
    }
}
