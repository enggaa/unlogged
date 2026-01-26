using UnityEngine;
using System;

namespace GameCore
{
    public class CharacterStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float staminaRegenDelay = 1f;

        [Header("Level & Stats")]
        [SerializeField] private int level = 1;
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defense = 5f;

        [Header("State")]
        [SerializeField] private bool isDead = false;
        [SerializeField] private bool isInvulnerable = false;

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showStaminaLogs = false;
        #endif

        private float _lastStaminaUseTime;
        private bool _isRegeneratingStamina = false; // 최적화: 상태 플래그

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnDeath;
        public event Action OnRevive;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public int Level => level;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            InitializeStats();
        }

        private void Update()
        {
            if (!isDead)
            {
                RegenerateStamina();
            }
        }

        private void InitializeStats()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            isDead = false;
            isInvulnerable = false;
            _lastStaminaUseTime = -staminaRegenDelay; // 시작 시 바로 회복 가능
        }

        #region Health Management

        public void TakeDamage(float damage, GameObject attacker = null)
        {
            if (isDead || isInvulnerable) return;

            // 방어력 적용 (최소 1 데미지)
            float finalDamage = Mathf.Max(damage - defense, 1f);
            
            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} took {finalDamage:F1} damage. HP: {currentHealth:F1}/{maxHealth}");
            #endif

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} healed {amount:F1}. HP: {currentHealth:F1}/{maxHealth}");
            #endif
        }

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0f;

            OnDeath?.Invoke();

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} has died!");
            #endif
        }

        public void Revive(float healthPercentage = 1f)
        {
            isDead = false;
            currentHealth = maxHealth * healthPercentage;
            currentStamina = maxStamina;

            OnRevive?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} has been revived!");
            #endif
        }

        #endregion

        #region Stamina Management

        public bool UseStamina(float amount)
        {
            if (currentStamina < amount)
            {
                return false;
            }

            currentStamina -= amount;
            currentStamina = Mathf.Max(currentStamina, 0f);
            _lastStaminaUseTime = Time.time;
            _isRegeneratingStamina = false; // 회복 중단

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            return true;
        }

        public bool HasStamina(float amount)
        {
            return currentStamina >= amount;
        }

        private void RegenerateStamina()
        {
            // 최적화: 이미 최대치면 연산 생략
            if (currentStamina >= maxStamina)
            {
                if (_isRegeneratingStamina)
                {
                    _isRegeneratingStamina = false;
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
                return;
            }

            // 딜레이 체크
            float timeSinceLastUse = Time.time - _lastStaminaUseTime;
            if (timeSinceLastUse < staminaRegenDelay)
            {
                #if UNITY_EDITOR
                // 최적화: 에디터에서만, 옵션 활성화 시에만 로그
                if (showStaminaLogs && !_isRegeneratingStamina)
                {
                    Debug.Log($"Waiting for regen: {timeSinceLastUse:F2}s / {staminaRegenDelay}s");
                }
                #endif
                return;
            }

            // 스태미나 회복
            if (!_isRegeneratingStamina)
            {
                _isRegeneratingStamina = true;
                #if UNITY_EDITOR
                if (showStaminaLogs)
                {
                    Debug.Log("Stamina regeneration started");
                }
                #endif
            }

            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            // 최적화: 0.1초마다만 이벤트 발생 (UI 업데이트 빈도 감소)
            if (Time.frameCount % 6 == 0) // 60fps 기준 0.1초
            {
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }

            #if UNITY_EDITOR
            if (showStaminaLogs && Time.frameCount % 30 == 0) // 0.5초마다만 로그
            {
                Debug.Log($"Regenerating stamina: {currentStamina:F1} / {maxStamina}");
            }
            #endif
        }

        public void RestoreStamina(float amount)
        {
            currentStamina += amount;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        #endregion

        #region Stat Modifications

        public void IncreaseMaxHealth(float amount)
        {
            maxHealth += amount;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void IncreaseMaxStamina(float amount)
        {
            maxStamina += amount;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public void IncreaseAttackPower(float amount)
        {
            attackPower += amount;
        }

        public void IncreaseDefense(float amount)
        {
            defense += amount;
        }

        public void LevelUp()
        {
            level++;
            
            // 레벨업 보너스
            IncreaseMaxHealth(10f);
            IncreaseMaxStamina(10f);
            IncreaseAttackPower(2f);
            IncreaseDefense(1f);

            // 체력/스태미나 완전 회복
            currentHealth = maxHealth;
            currentStamina = maxStamina;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            Debug.Log($"{gameObject.name} leveled up to {level}!");
        }

        #endregion

        // 에디터 전용 디버그 UI
        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showStaminaLogs) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.cyan;
            style.fontSize = 11;

            GUI.Label(new Rect(10, 50, 300, 20), 
                $"Stamina: {currentStamina:F1}/{maxStamina} | Regen: {_isRegeneratingStamina}", style);
        }
        #endif
    }
}