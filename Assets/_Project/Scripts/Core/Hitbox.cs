using UnityEngine;
using System.Collections.Generic;

namespace GameCore
{
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [Header("Hitbox Settings")]
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private bool isActive = false;

        [Header("Timing")]
        [SerializeField] private bool singleHit = true; // 한 번만 히트
        
        private Collider _collider;
        private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();
        private GameObject _owner;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        public void Initialize(GameObject owner, float damage, DamageType type)
        {
            _owner = owner;
            damageAmount = damage;
            damageType = type;
        }

        public void ActivateHitbox()
        {
            isActive = true;
            _collider.enabled = true;
            _hitTargets.Clear();
        }

        public void DeactivateHitbox()
        {
            isActive = false;
            _collider.enabled = false;
            _hitTargets.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;

            // 레이어 체크
            if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

            // 자기 자신은 공격 안함
            if (other.gameObject == _owner || other.transform.root.gameObject == _owner) return;

            // 이미 맞은 대상은 다시 안맞음 (singleHit일 경우)
            if (singleHit && _hitTargets.Contains(other.gameObject)) return;

            // Damageable 컴포넌트 찾기
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                // 데미지 데이터 생성
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitDirection = (other.transform.position - transform.position).normalized;

                DamageData damageData = new DamageData(
                    damageAmount,
                    damageType,
                    _owner,
                    hitPoint,
                    hitDirection
                );

                // 데미지 적용
                damageable.TakeDamage(damageData);

                _hitTargets.Add(other.gameObject);

                Debug.Log($"Hitbox hit {other.gameObject.name} for {damageAmount} damage");
            }
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = isActive ? Color.red : Color.yellow;
                
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position, sphere.radius);
                }
            }
        }
    }
}