using UnityEngine;
using UnityEngine.AI;
using GameCore;

namespace GameCore.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(DamageableEntity))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Combat")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 1f;

        [Header("Movement")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float patrolSpeed = 2f;

        [Header("Optimization")]
        [SerializeField] private float pathUpdateInterval = 0.25f; // 경로 업데이트 간격
        [SerializeField] private float distanceCheckInterval = 0.1f; // 거리 체크 간격

        private NavMeshAgent _agent;
        private CharacterStats _stats;
        private Transform _player;
        private float _lastAttackTime;
        private EnemyState _currentState = EnemyState.Idle;

        // 최적화: 타이머 캐싱
        private float _nextPathUpdateTime;
        private float _nextDistanceCheckTime;
        private float _cachedDistanceToPlayer;

        // 최적화: OverlapSphere 결과 재사용
        private Collider[] _hitResults = new Collider[5];

        // 최적화: Vector3 재사용
        private Vector3 _directionToPlayer;

        private enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Dead
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _stats = GetComponent<CharacterStats>();
        }

        private void Start()
        {
            // 플레이어 찾기 (최적화: Tag 사용)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"{gameObject.name}: Player not found! Tag the player with 'Player'");
                #endif
            }

            // Stats 이벤트 구독
            _stats.OnDeath += OnDeath;

            // Attack Point 생성
            if (attackPoint == null)
            {
                GameObject ap = new GameObject("AttackPoint");
                ap.transform.SetParent(transform);
                ap.transform.localPosition = new Vector3(0, 1, 1);
                attackPoint = ap.transform;
            }

            // 초기화
            _nextPathUpdateTime = Time.time;
            _nextDistanceCheckTime = Time.time;
        }

        private void Update()
        {
            if (_stats.IsDead)
            {
                _currentState = EnemyState.Dead;
                return;
            }

            if (_player == null) return;

            // 최적화: 거리 체크를 일정 간격마다만 수행
            if (Time.time >= _nextDistanceCheckTime)
            {
                _cachedDistanceToPlayer = Vector3.Distance(transform.position, _player.position);
                _nextDistanceCheckTime = Time.time + distanceCheckInterval;
            }

            // 상태 업데이트
            switch (_currentState)
            {
                case EnemyState.Idle:
                    UpdateIdleState();
                    break;
                case EnemyState.Chase:
                    UpdateChaseState();
                    break;
                case EnemyState.Attack:
                    UpdateAttackState();
                    break;
            }
        }

        private void UpdateIdleState()
        {
            if (_cachedDistanceToPlayer <= detectionRange)
            {
                _currentState = EnemyState.Chase;
                _agent.speed = chaseSpeed;
                _agent.isStopped = false;
            }
        }

        private void UpdateChaseState()
        {
            // 범위 체크
            if (_cachedDistanceToPlayer > detectionRange * 1.5f)
            {
                _currentState = EnemyState.Idle;
                _agent.ResetPath();
                _agent.isStopped = true;
                return;
            }

            if (_cachedDistanceToPlayer <= attackRange)
            {
                _currentState = EnemyState.Attack;
                _agent.ResetPath();
                _agent.isStopped = true;
                return;
            }

            // 최적화: 경로 업데이트를 일정 간격마다만 수행
            if (Time.time >= _nextPathUpdateTime)
            {
                _agent.SetDestination(_player.position);
                _nextPathUpdateTime = Time.time + pathUpdateInterval;
            }

            // 플레이어 바라보기 (최적화: 방향 벡터 재사용)
            _directionToPlayer = _player.position - transform.position;
            _directionToPlayer.y = 0;
            
            if (_directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(_directionToPlayer);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    lookRotation, 
                    Time.deltaTime * 5f
                );
            }
        }

        private void UpdateAttackState()
        {
            if (_cachedDistanceToPlayer > attackRange * 1.2f)
            {
                _currentState = EnemyState.Chase;
                _agent.isStopped = false;
                return;
            }

            // 플레이어 바라보기
            _directionToPlayer = _player.position - transform.position;
            _directionToPlayer.y = 0;
            
            if (_directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(_directionToPlayer);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    lookRotation, 
                    Time.deltaTime * 10f
                );
            }

            // 공격 쿨다운 체크
            if (Time.time - _lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        private void PerformAttack()
        {
            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} attacks!");
            #endif

            // 최적화: OverlapSphereNonAlloc 사용
            int hitCount = Physics.OverlapSphereNonAlloc(
                attackPoint.position, 
                attackRadius, 
                _hitResults, 
                playerLayer
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _hitResults[i];
                
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead())
                {
                    Vector3 hitDirection = (col.transform.position - transform.position).normalized;
                    
                    DamageData damageData = new DamageData(
                        attackDamage,
                        DamageType.Physical,
                        gameObject,
                        attackPoint.position,
                        hitDirection
                    );

                    damageable.TakeDamage(damageData);
                }
            }

            // 최적화: 배열 초기화
            System.Array.Clear(_hitResults, 0, hitCount);
        }

        private void OnDeath()
        {
            _currentState = EnemyState.Dead;
            _agent.enabled = false;
            
            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} died!");
            #endif
            
            // 죽는 애니메이션이나 이펙트 (나중에)
            // 최적화: Object Pool로 반환하는 것이 좋음
            Destroy(gameObject, 3f);
        }

        // 최적화: 거리 기반 활성화/비활성화 (외부에서 호출)
        public void SetAIUpdateEnabled(bool enabled)
        {
            this.enabled = enabled;
            _agent.enabled = enabled;
        }

        private void OnDrawGizmosSelected()
        {
            // 감지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // 공격 판정
            if (attackPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제 (메모리 누수 방지)
            if (_stats != null)
            {
                _stats.OnDeath -= OnDeath;
            }
        }
    }
}

/*
=== 추가 최적화 제안 ===

1. **거리 기반 AI 매니저**
```csharp
public class AIManager : MonoBehaviour {
    void Update() {
        foreach (var enemy in enemies) {
            float dist = Vector3.Distance(player.position, enemy.position);
            
            if (dist > 50f) {
                enemy.SetAIUpdateEnabled(false); // AI 완전 중지
            } else if (dist > 30f) {
                enemy.pathUpdateInterval = 0.5f; // 느린 업데이트
            } else {
                enemy.pathUpdateInterval = 0.25f; // 정상 업데이트
            }
        }
    }
}
```

2. **Object Pooling**
```csharp
// OnDeath()에서 Destroy 대신
EnemyPoolManager.Instance.ReturnEnemy(this);
```

3. **NavMesh 베이킹**
- 정적 오브젝트를 Navigation Static으로 설정
- NavMesh 베이킹 (Window > AI > Navigation)
- Agent Radius, Height 최적화
*/