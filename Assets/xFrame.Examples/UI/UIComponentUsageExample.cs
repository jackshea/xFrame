using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.UI;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// UI组件使用示例
    /// 演示组件复用、父子通讯、生命周期传递
    /// </summary>
    public class UIComponentUsageExample : MonoBehaviour
    {
        private IUIManager _uiManager;

        private async void Start()
        {
            Debug.Log("=== UI组件系统使用示例 ===\n");

            await Task.Delay(1000);

            // 示例1: 组件复用
            Example1_ComponentReuse();

            await Task.Delay(2000);

            // 示例2: 父子通讯
            Example2_ParentChildCommunication();

            await Task.Delay(2000);

            // 示例3: 生命周期传递
            Example3_LifecyclePropagation();

            await Task.Delay(2000);

            // 示例4: 组件ID识别
            Example4_ComponentIdIdentification();
        }

        [Inject]
        public void Construct(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        /// <summary>
        /// 示例1: 组件复用
        /// 演示如何创建和使用多个相同类型的组件
        /// </summary>
        private void Example1_ComponentReuse()
        {
            Debug.Log("\n【示例1】组件复用");
            Debug.Log("说明：创建多个相同类型的ItemSlotComponent，每个都有唯一的ComponentId");

            // 通过UIManager打开背包面板
            // var inventory = await _uiManager.OpenAsync<InventoryPanel>();

            // 背包面板会自动创建多个ItemSlotComponent
            // 每个组件都有唯一的ComponentId，可以独立管理

            Debug.Log("✅ 组件复用允许同一组件类型创建多个实例");
            Debug.Log("✅ 每个实例通过ComponentId唯一标识");
        }

        /// <summary>
        /// 示例2: 父子通讯
        /// 演示父子组件之间的通讯方式
        /// </summary>
        private void Example2_ParentChildCommunication()
        {
            Debug.Log("\n【示例2】父子通讯");
            Debug.Log("");
            Debug.Log("子 → 父 通讯（事件总线）：");
            Debug.Log("1. 子组件(ItemSlotComponent)点击");
            Debug.Log("2. 调用 SendEvent<ItemSlotClickedEvent>(...)");
            Debug.Log("3. 事件自动包装为 UIComponentEventWrapper");
            Debug.Log("4. 通过事件总线发送全局事件");
            Debug.Log("5. 父组件(InventoryPanel)订阅并接收");
            Debug.Log("");
            Debug.Log("父 → 子 通讯（直接调用）：");
            Debug.Log("1. 父组件持有子组件引用 List<ItemSlotComponent>");
            Debug.Log("2. 父组件直接调用: slot.SetData(data)");
            Debug.Log("3. 父组件直接调用: slot.Show()");
            Debug.Log("");
            Debug.Log("关键点：");
            Debug.Log("- 子组件不知道父组件（完全解耦）");
            Debug.Log("- 父组件知道子组件（可以直接调用）");
            Debug.Log("- 事件包含ComponentId，可识别具体子组件");
        }

        /// <summary>
        /// 示例3: 生命周期传递
        /// 演示父组件生命周期如何影响子组件
        /// </summary>
        private void Example3_LifecyclePropagation()
        {
            Debug.Log("\n【示例3】生命周期传递");
            Debug.Log("生命周期传递规则：");
            Debug.Log("");
            Debug.Log("父UI.OnShow() → ComponentManager.OnParentShow()");
            Debug.Log("  └─> 所有可见子组件.Show()");
            Debug.Log("");
            Debug.Log("父UI.OnHide() → ComponentManager.OnParentHide()");
            Debug.Log("  └─> 所有可见子组件.Hide()");
            Debug.Log("");
            Debug.Log("父UI.OnClose() → ComponentManager.OnParentClose()");
            Debug.Log("  └─> 所有子组件.Reset()");
            Debug.Log("");
            Debug.Log("父UI.OnDestroy() → ComponentManager.OnParentDestroy()");
            Debug.Log("  └─> 所有子组件.DestroyComponent()");
            Debug.Log("");
            Debug.Log("优势：");
            Debug.Log("✅ 子组件状态自动跟随父UI");
            Debug.Log("✅ 无需手动管理每个子组件的生命周期");
        }

        /// <summary>
        /// 示例4: 组件ID识别
        /// 演示如何通过ComponentId区分同类型的多个组件
        /// </summary>
        private void Example4_ComponentIdIdentification()
        {
            Debug.Log("\n【示例4】组件ID识别");
            Debug.Log("场景：背包有20个ItemSlotComponent");
            Debug.Log("问题：用户点击了哪个槽位？");
            Debug.Log("");
            Debug.Log("解决方案 - ComponentId：");
            Debug.Log("1. 每个ItemSlotComponent创建时自动生成唯一ID");
            Debug.Log("   格式: 'ItemSlotComponent_a1b2c3d4e5f6...'");
            Debug.Log("");
            Debug.Log("2. 点击事件自动携带ComponentId");
            Debug.Log("   UIComponentEventWrapper.ComponentId = 'ItemSlotComponent_xxx'");
            Debug.Log("");
            Debug.Log("3. 父组件通过ComponentId获取具体组件");
            Debug.Log("   var slot = ComponentManager.GetComponent<ItemSlotComponent>(componentId);");
            Debug.Log("");
            Debug.Log("优势：");
            Debug.Log("✅ 无需手动为每个组件分配ID");
            Debug.Log("✅ 支持运行时动态创建组件");
            Debug.Log("✅ 类型安全（泛型约束）");
        }
    }

    #region 完整的组件通讯示例

    /// <summary>
    /// 技能按钮组件示例
    /// 展示完整的组件生命周期和事件通讯
    /// </summary>
    public class SkillButtonComponent : UIComponent
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image icon;

        [SerializeField]
        private TextMeshProUGUI cooldownText;

        private float _cooldownRemaining;

        private SkillData _skillData;

        #region 事件处理

        private void OnButtonClicked()
        {
            if (_cooldownRemaining > 0)
            {
                Debug.Log($"[SkillButton] 技能冷却中: {_skillData?.Name}");
                return;
            }

            Debug.Log($"[SkillButton] 技能释放: {_skillData?.Name}");

            // 发送事件（通过事件总线）
            SendEvent(new SkillButtonClickedEvent
            {
                SkillId = _skillData.Id,
                SkillName = _skillData.Name
            });

            // 开始冷却
            StartCooldown();
        }

        #endregion

        #region 生命周期

        protected override void OnInitialize()
        {
            base.OnInitialize();
            button?.onClick.AddListener(OnButtonClicked);
            Debug.Log($"[SkillButton] 初始化: {ComponentId}");
        }

        protected override void OnSetData(object data)
        {
            base.OnSetData(data);

            if (data is SkillData skillData)
            {
                _skillData = skillData;
                UpdateDisplay();
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log($"[SkillButton] 显示: {_skillData?.Name}");
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log($"[SkillButton] 隐藏: {_skillData?.Name}");
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            UpdateDisplay();
        }

        protected override void OnReset()
        {
            base.OnReset();
            _skillData = null;
            _cooldownRemaining = 0;
        }

        protected override void OnDestroyComponent()
        {
            button?.onClick.RemoveListener(OnButtonClicked);
            base.OnDestroyComponent();
            Debug.Log($"[SkillButton] 销毁: {ComponentId}");
        }

        #endregion

        #region 显示更新

        private void UpdateDisplay()
        {
            if (_skillData == null) return;

            if (icon != null)
                icon.sprite = _skillData.Icon;

            UpdateCooldownDisplay();
        }

        private void UpdateCooldownDisplay()
        {
            if (cooldownText != null)
            {
                if (_cooldownRemaining > 0)
                    cooldownText.text = _cooldownRemaining.ToString("F1");
                else
                    cooldownText.text = "";
            }
        }

        #endregion

        #region 冷却管理

        private void StartCooldown()
        {
            _cooldownRemaining = _skillData?.Cooldown ?? 0;
        }

        private void Update()
        {
            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining -= Time.deltaTime;
                if (_cooldownRemaining < 0)
                    _cooldownRemaining = 0;

                UpdateCooldownDisplay();
            }
        }

        #endregion
    }

    /// <summary>
    /// 技能栏面板示例
    /// 展示如何管理多个技能按钮组件
    /// </summary>
    public class SkillBarPanel : UIPanel
    {
        [SerializeField]
        private Transform skillContainer;

        [SerializeField]
        private SkillButtonComponent skillButtonPrefab;

        private readonly List<SkillButtonComponent> _skillButtons = new();

        #region 生命周期

        protected override void OnCreate()
        {
            base.OnCreate();

            // 创建技能按钮
            CreateSkillButtons();

            // 订阅组件事件
            xFrameEventBus.SubscribeTo<UIComponentEventWrapper<SkillButtonClickedEvent>>(
                OnSkillButtonClicked);

            Debug.Log("[SkillBarPanel] 创建完成");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            if (data is List<SkillData> skills) LoadSkills(skills);
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("[SkillBarPanel] 显示技能栏");
            // ComponentManager会自动调用所有子组件的Show()
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("[SkillBarPanel] 隐藏技能栏");
            // ComponentManager会自动调用所有子组件的Hide()
        }

        protected override void OnUIDestroy()
        {
            xFrameEventBus.UnsubscribeFrom<UIComponentEventWrapper<SkillButtonClickedEvent>>(
                OnSkillButtonClicked);
            base.OnUIDestroy();
        }

        #endregion

        #region 组件管理

        private void CreateSkillButtons()
        {
            for (var i = 0; i < 4; i++)
            {
                var button = Instantiate(skillButtonPrefab, skillContainer);
                ComponentManager.RegisterComponent(button);
                _skillButtons.Add(button);
                button.Show();
            }

            Debug.Log($"[SkillBarPanel] 创建了 {_skillButtons.Count} 个技能按钮");
        }

        private void LoadSkills(List<SkillData> skills)
        {
            for (var i = 0; i < skills.Count && i < _skillButtons.Count; i++) _skillButtons[i].SetData(skills[i]);
        }

        #endregion

        #region 事件处理

        private void OnSkillButtonClicked(
            ref UIComponentEventWrapper<SkillButtonClickedEvent> wrapper)
        {
            Debug.Log("[SkillBarPanel] 技能按钮点击: " +
                      $"ComponentId={wrapper.ComponentId}, " +
                      $"SkillId={wrapper.Event.SkillId}, " +
                      $"SkillName={wrapper.Event.SkillName}");

            // 通过ComponentId获取具体的按钮组件
            var button = ComponentManager.GetComponent<SkillButtonComponent>(wrapper.ComponentId);

            // 执行技能逻辑
            UseSkill(wrapper.Event.SkillId);
        }

        private void UseSkill(int skillId)
        {
            Debug.Log($"[SkillBarPanel] 使用技能: SkillId={skillId}");
            // 实际的技能使用逻辑...
        }

        #endregion
    }

    #endregion

    #region 数据类型

    /// <summary>
    /// 技能数据
    /// </summary>
    public class SkillData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Sprite Icon { get; set; }
        public float Cooldown { get; set; }
    }

    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    public struct SkillButtonClickedEvent : IUIComponentEvent
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
    }

    #endregion
}