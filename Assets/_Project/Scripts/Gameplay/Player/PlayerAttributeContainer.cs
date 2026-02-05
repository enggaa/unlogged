using UnityEngine;

namespace BrightSouls.Gameplay
{
    public sealed class PlayerAttributeContainer : MonoBehaviour, IAttributesContainerOwner
    {
        /* ------------------------------- Properties ------------------------------- */

        public PoiseAttribute Poise
        {
            get => attributes.GetAttribute<PoiseAttribute>();
        }

        public MaxPoiseAttribute MaxPoise
        {
            get => attributes.GetAttribute<MaxPoiseAttribute>();
        }

        public StaminaAttribute Stamina
        {
            get => attributes.GetAttribute<StaminaAttribute>();
        }

        public MaxStaminaAttribute MaxStamina
        {
            get => attributes.GetAttribute<MaxStaminaAttribute>();
        }

        public HealthAttribute Health
        {
            get => attributes.GetAttribute<HealthAttribute>();
        }

        public MaxHealthAttribute MaxHealth
        {
            get => attributes.GetAttribute<MaxHealthAttribute>();
        }

        public FactionAttribute Faction
        {
            get => attributes.GetAttribute<FactionAttribute>();
        }

        public StatusAttribute Status
        {
            get => attributes.GetAttribute<StatusAttribute>();
        }

        public AttributesContainer Attributes
        {
            get => attributes;
        }

        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [SerializeField] private PlayerAttributeData data;

        /* ----------------------------- Runtime Fields ----------------------------- */

        private AttributesContainer attributes;

        /* ------------------------------ Unity Events ------------------------------ */

        private void Start()
        {
            InitializeAttributes();
        }

        /* ----------------------------- Initialization ----------------------------- */

        private void InitializeAttributes()
        {
            if (data == null)
            {
                Debug.LogError("PlayerAttributeContainer is missing PlayerAttributeData. Using default values.");
            }

            float healthValue = data != null ? data.Health : 100f;
            float maxHealthValue = data != null ? data.MaxHealth : 100f;
            float staminaValue = data != null ? data.Stamina : 100f;
            float maxStaminaValue = data != null ? data.MaxStamina : 100f;
            float poiseValue = data != null ? data.Poise : 50f;
            float maxPoiseValue = data != null ? data.MaxPoise : 50f;

            var health = new HealthAttribute(healthValue);
            var maxHealth = new MaxHealthAttribute(maxHealthValue);
            var stamina = new StaminaAttribute(staminaValue);
            var maxStamina = new MaxStaminaAttribute(maxStaminaValue);
            var poise = new PoiseAttribute(poiseValue);
            var maxPoise = new MaxPoiseAttribute(maxPoiseValue);
            var faction = new FactionAttribute();
            var status = new StatusAttribute();

            attributes = new AttributesContainer();
            attributes.AddAttribute<HealthAttribute>(health);
            attributes.AddAttribute<MaxHealthAttribute>(maxHealth);
            attributes.AddAttribute<StaminaAttribute>(stamina);
            attributes.AddAttribute<MaxStaminaAttribute>(maxStamina);
            attributes.AddAttribute<PoiseAttribute>(poise);
            attributes.AddAttribute<MaxPoiseAttribute>(maxPoise);
            attributes.AddAttribute<FactionAttribute>(faction);
            attributes.AddAttribute<StatusAttribute>(status);
        }

        /* -------------------------------------------------------------------------- */
    }
}