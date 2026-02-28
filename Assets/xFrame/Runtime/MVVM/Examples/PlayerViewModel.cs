using xFrame.Runtime.MVVM.Core;

namespace xFrame.Runtime.MVVM.Examples
{
    /// <summary>
    /// 玩家视图模型。
    /// 负责将模型数据转换为 UI 可直接消费的状态。
    /// </summary>
    public sealed class PlayerViewModel : ViewModelBase
    {
        private readonly PlayerModel _model;

        public PlayerViewModel(PlayerModel model)
        {
            _model = model;

            Name = new BindableProperty<string>(string.Empty);
            HealthFillAmount = new BindableProperty<float>(1f);
            HealthText = new BindableProperty<string>(string.Empty);
            TakeDamageCommand = new RelayCommand(TakeDamage, () => _model.Health > 0);

            UpdateProperties();
        }

        public BindableProperty<string> Name { get; }

        public BindableProperty<float> HealthFillAmount { get; }

        public BindableProperty<string> HealthText { get; }

        public RelayCommand TakeDamageCommand { get; }

        public void TakeDamage()
        {
            TakeDamage(15);
        }

        public void TakeDamage(int damage)
        {
            _model.TakeDamage(damage);
            UpdateProperties();
            TakeDamageCommand.RaiseCanExecuteChanged();
        }

        private void UpdateProperties()
        {
            Name.Value = _model.PlayerName;
            HealthFillAmount.Value = _model.MaxHealth > 0 ? (float)_model.Health / _model.MaxHealth : 0f;
            HealthText.Value = $"{_model.Health} / {_model.MaxHealth}";
        }
    }
}
