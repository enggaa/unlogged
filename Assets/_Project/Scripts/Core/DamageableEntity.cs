using UnityEngine;

namespace GameCore
{
    [RequireComponent(typeof(CharacterStats))]
    public class DamageableEntity : MonoBehaviour, IDamageable
    {
        [Header("Damage Settings")]
        [SerializeField] private bool showDamageLog = true;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        private CharacterStats _stats;
        private bool _isInvulnerable = false;
        private float _invulnerabilityTimer = 0f;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        private void Update()
        {
            if (_isInvulnerable)
            {
                _invulnerabilityTimer -= Time.deltaTime;
                if (_invulnerabilityTimer <= 0f)
                {
                    _isInvulnerable = false;
                    _stats.SetInvulnerable(false);
                }
            }
        }

        public void TakeDamage(DamageData damageData)
        {
            if (_stats.IsDead || _isInvulnerable) return;

            // 데미지 타입에 따른 추가 로직 가능
            float finalDamage = CalculateDamage(damageData);

            _stats.TakeDamage(finalDamage, damageData.attacker);

            if (showDamageLog)
            {
                Debug.Log($"{gameObject.name} received {finalDamage} {damageData.damageType} damage from {damageData.attacker?.name ?? "Unknown"}");
            }

            // 피격 효과 (나중에 추가)
            OnDamageReceived(damageData);

            // 무적 시간 설정
            if (invulnerabilityDuration > 0f)
            {
                SetInvulnerable(invulnerabilityDuration);
            }
        }

        private float CalculateDamage(DamageData damageData)
        {
            float damage = damageData.damageAmount;

            // 데미지 타입별 계산 (나중에 확장 가능)
            switch (damageData.damageType)
            {
                case DamageType.Physical:
                    // 물리 방어력 적용
                    break;
                case DamageType.Magic:
                    // 마법 저항력 적용 (나중에)
                    break;
                case DamageType.Fire:
                    // 화염 저항력 적용
                    break;
            }

            return damage;
        }

        private void OnDamageReceived(DamageData damageData)
        {
            // 피격 이펙트
            // 피격 사운드
            // 넉백 처리 (나중에)
            
            // 예시: 피격 방향 계산
            if (damageData.hitDirection != Vector3.zero)
            {
                // 넉백이나 경직 처리
            }
        }

        public void SetInvulnerable(float duration)
        {
            _isInvulnerable = true;
            _invulnerabilityTimer = duration;
            _stats.SetInvulnerable(true);
        }

        public bool IsDead()
        {
            return _stats.IsDead;
        }
    }
}