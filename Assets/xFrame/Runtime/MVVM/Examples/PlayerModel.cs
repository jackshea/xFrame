namespace xFrame.Runtime.MVVM.Examples
{
    /// <summary>
    /// 玩家数据模型。
    /// 仅关注业务数据，不依赖 View 或 ViewModel。
    /// </summary>
    public sealed class PlayerModel
    {
        public PlayerModel(string playerName, int maxHealth)
        {
            PlayerName = playerName;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public string PlayerName { get; private set; }

        public int Health { get; private set; }

        public int MaxHealth { get; private set; }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            Health -= damage;
            if (Health < 0)
            {
                Health = 0;
            }
        }
    }
}
