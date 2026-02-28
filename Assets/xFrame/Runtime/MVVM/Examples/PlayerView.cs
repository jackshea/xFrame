using TMPro;
using UnityEngine;
using UnityEngine.UI;
using xFrame.Runtime.MVVM.Core;

namespace xFrame.Runtime.MVVM.Examples
{
    /// <summary>
    /// 玩家视图。
    /// 负责控件引用与 ViewModel 绑定，不包含业务计算逻辑。
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        private TextMeshProUGUI _healthText;

        [SerializeField]
        private Image _healthBar;

        [SerializeField]
        private Button _damageButton;

        private BindingContext _bindingContext;
        private PlayerViewModel _viewModel;

        private void Start()
        {
            var model = new PlayerModel("Hero", 100);
            _viewModel = new PlayerViewModel(model);
            BindViewModel(_viewModel);
        }

        private void OnDestroy()
        {
            UnbindViewModel();
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void BindViewModel(PlayerViewModel viewModel)
        {
            _bindingContext = new BindingContext();

            _bindingContext.Add(viewModel.Name.Bind(OnNameChanged));
            _bindingContext.Add(viewModel.HealthText.Bind(OnHealthTextChanged));
            _bindingContext.Add(viewModel.HealthFillAmount.Bind(OnHealthFillAmountChanged));

            _damageButton.onClick.AddListener(viewModel.TakeDamage);
        }

        private void UnbindViewModel()
        {
            _bindingContext?.Dispose();
            _bindingContext = null;

            if (_damageButton != null)
            {
                _damageButton.onClick.RemoveAllListeners();
            }
        }

        private void OnNameChanged(string newValue)
        {
            if (_nameText != null)
            {
                _nameText.text = newValue;
            }
        }

        private void OnHealthTextChanged(string newValue)
        {
            if (_healthText != null)
            {
                _healthText.text = newValue;
            }
        }

        private void OnHealthFillAmountChanged(float value)
        {
            if (_healthBar != null)
            {
                _healthBar.fillAmount = value;
            }
        }
    }
}
