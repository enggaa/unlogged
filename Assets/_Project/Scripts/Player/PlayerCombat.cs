using UnityEngine;
using GameCore.Managers;

namespace GameCore.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float lightAttackMultiplier = 1.0f;
        [SerializeField] private float heavyAttackMultiplier = 2.5f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private LayerMask enemyLayers;

        [Header("Combo System")]
        [SerializeField] private float comboWindow = 1f;
        [SerializeField] private int maxComboCount = 3;
        [SerializeField] private float[] comboBonusMultipliers = new float[] { 1.0f, 1.2f, 1.5f };

        [Header("Stamina Cost")]
        [SerializeField] private float attackStaminaCost = 15f;
        [SerializeField] private float heavyAttackStaminaCost = 30f;

        private InputManager _input;
        private CharacterStats _stats;
        private Animator _animator;

        private int _currentComboCount = 0;
        private float _lastAttackTime;
        private bool _canAttack = true;

        // 최적화: 해시 캐싱
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int ComboCountHash = Animator.StringToHash("ComboCount");
        private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");

        // 최적화: OverlapSphere 결과 재사용
        private Collider[] _hitResults = new Collider[10];

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _input = GameManager.Instance.InputManager;

            if (attackPoint == null)
            {
                GameObject ap = new GameObject("AttackPoint");
                ap.transform.SetParent(transform);
                ap.transform.localPosition = new Vector3(0, 1, 1);
                attackPoint = ap.transform;
            }
        }

        private void Update()
        {
            HandleAttackInput();
            UpdateComboTimer();
        }

        private void HandleAttackInput()
        {
            if (!_canAttack || _stats.IsDead) return;

            if (_input.AttackPressed)
            {
                if (_stats.HasStamina(attackStaminaCost))
                {
                    PerformAttack();
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.Log("Not enough stamina!");
                }
                #endif
            }

            if (_input.HeavyAttackPressed)
            {
                if (_stats.HasStamina(heavyAttackStaminaCost))
                {
                    PerformHeavyAttack();
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.Log("Not enough stamina for heavy attack!");
                }
                #endif
            }
        }

        private void PerformAttack()
        {
            _stats.UseStamina(attackStaminaCost);

            // 콤보 카운트 계산
            if (Time.time - _lastAttackTime <= comboWindow)
            {
                _currentComboCount++;
                if (_currentComboCount > maxComboCount)
                {
                    _currentComboCount = 1;
                }
            }
            else
            {
                _currentComboCount = 1;
            }

            _lastAttackTime = Time.time;

            #if UNITY_EDITOR
            Debug.Log($"Player attacks! Combo: {_currentComboCount}");
            #endif

            // 애니메이션 트리거
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
                _animator.SetInteger(ComboCountHash, _currentComboCount);
            }

            // 최적화: Invoke 대신 직접 호출
            // Animation Event를 사용하는 것이 더 좋지만, 없을 경우 대비
            // 실제로는 Animation Event에서 DealLightDamage()를 호출해야 함
        }

        private void PerformHeavyAttack()
        {
            _stats.UseStamina(heavyAttackStaminaCost);
            _currentComboCount = 0;

            #if UNITY_EDITOR
            Debug.Log("Player performs heavy attack!");
            #endif

            if (_animator != null)
            {
                _animator.SetTrigger(HeavyAttackHash);
            }

            // 최적화: Animation Event 사용 권장
        }

        // Animation Event에서 호출할 메서드
        public void OnLightAttackHit()
        {
            DealLightDamage();
        }

        public void OnHeavyAttackHit()
        {
            DealHeavyDamage();
        }

        private void DealLightDamage()
        {
            // 콤보 보너스 계산
            float comboMultiplier = 1.0f;
            if (_currentComboCount > 0 && _currentComboCount <= comboBonusMultipliers.Length)
            {
                comboMultiplier = comboBonusMultipliers[_currentComboCount - 1];
            }

            // 최종 데미지
            float finalDamage = _stats.AttackPower * lightAttackMultiplier * comboMultiplier;

            DealDamage(finalDamage, attackRadius);
        }

        private void DealHeavyDamage()
        {
            float finalDamage = _stats.AttackPower * heavyAttackMultiplier;
            DealDamage(finalDamage, attackRadius * 1.5f);
        }

        private void DealDamage(float damage, float radius)
        {
            // 최적화: OverlapSphereNonAlloc 사용
            int hitCount = Physics.OverlapSphereNonAlloc(
                attackPoint.position, 
                radius, 
                _hitResults, 
                enemyLayers
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider enemy = _hitResults[i];
                
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead())
                {
                    Vector3 hitDirection = (enemy.transform.position - transform.position).normalized;

                    DamageData damageData = new DamageData(
                        damage,
                        DamageType.Physical,
                        gameObject,
                        attackPoint.position,
                        hitDirection
                    );

                    damageable.TakeDamage(damageData);

                    #if UNITY_EDITOR
                    Debug.Log($"Hit {enemy.name} for {damage:F1} damage!");
                    #endif
                }
            }

            // 최적화: 배열 초기화 (다음 사용을 위해)
            System.Array.Clear(_hitResults, 0, hitCount);
        }

        private void UpdateComboTimer()
        {
            // 최적화: 콤보가 있을 때만 체크
            if (_currentComboCount > 0 && Time.time - _lastAttackTime > comboWindow)
            {
                _currentComboCount = 0;
                
                if (_animator != null)
                {
                    _animator.SetInteger(ComboCountHash, 0);
                }
            }
        }

        public void SetCanAttack(bool canAttack)
        {
            _canAttack = canAttack;
        }

        // Animation Event에서 호출: 공격 시작
        public void OnAttackStart()
        {
            _canAttack = false;
        }

        // Animation Event에서 호출: 공격 종료
        public void OnAttackEnd()
        {
            _canAttack = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius * 1.5f);
        }
    }
}

/*
=== Animation Event 설정 방법 ===

1. 애니메이션 클립 선택 (예: LightAttack1, LightAttack2, HeavyAttack)
2. Animation 창에서 타임라인의 적절한 시점에 Event 추가
3. Event 함수 설정:
   - 공격 시작 시점: OnAttackStart()
   - 데미지 판정 시점: OnLightAttackHit() 또는 OnHeavyAttackHit()
   - 공격 종료 시점: OnAttackEnd()

이렇게 하면 Invoke() 대신 정확한 타이밍에 데미지를 입힐 수 있습니다!
*/