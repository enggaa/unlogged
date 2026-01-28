using UnityEngine;
using System.Collections.Generic;
using GameCore.Managers;

namespace GameCore.Player
{
    /// <summary>
    /// 소울류 게임의 락온 시스템
    /// Tab 키로 가장 가까운 적을 락온/해제
    /// </summary>
    public class LockOnSystem : MonoBehaviour
    {
        [Header("Lock-On Settings")]
        [SerializeField] private float lockOnRange = 15f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float targetLostDistance = 20f;
        
        [Header("References")]
        [SerializeField] private Transform cameraRig;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Transform _currentTarget;
        private bool _isLockedOn = false;
        private InputManager _input;
        private UnityEngine.Camera _mainCamera; // ← UnityEngine 명시!
        private Collider[] _enemiesBuffer = new Collider[20];
        private GameCore.Camera.TPSCameraController _cameraController;
        
        public bool IsLockedOn => _isLockedOn;
        public Transform CurrentTarget => _currentTarget;
        
        public System.Action<Transform> OnLockOn;
        public System.Action OnLockOff;
        
        private void Start()
        {
            _input = GameManager.Instance.InputManager;
            _mainCamera = UnityEngine.Camera.main; // ← UnityEngine 명시!
            
            // CameraRig 자동 찾기
            if (cameraRig == null)
            {
                GameObject rigObj = GameObject.Find("CameraRig");
                if (rigObj != null)
                {
                    cameraRig = rigObj.transform;
                    _cameraController = cameraRig.GetComponent<GameCore.Camera.TPSCameraController>();
                }
            }
            else
            {
                _cameraController = cameraRig.GetComponent<GameCore.Camera.TPSCameraController>();
            }
        }
        
        private void Update()
        {
            if (_input.LockOnPressed)
            {
                if (_isLockedOn)
                    ReleaseLockOn();
                else
                    TryLockOn();
            }
            
            if (_isLockedOn)
            {
                UpdateLockOn();
            }
        }
        
        private void TryLockOn()
        {
            Transform closestEnemy = FindClosestEnemy();
            
            if (closestEnemy != null)
            {
                _currentTarget = closestEnemy;
                _isLockedOn = true;
                
                // 카메라에 락온 알림
                if (_cameraController != null)
                {
                    _cameraController.SetLockOnTarget(_currentTarget);
                }
                
                OnLockOn?.Invoke(_currentTarget);
                
                #if UNITY_EDITOR
                if (showDebugInfo)
                    Debug.Log($"락온: {_currentTarget.name}");
                #endif
            }
        }
        
        private Transform FindClosestEnemy()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                lockOnRange,
                _enemiesBuffer,
                enemyLayer
            );
            
            if (hitCount == 0) return null;
            
            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            
            for (int i = 0; i < hitCount; i++)
            {
                Transform enemy = _enemiesBuffer[i].transform;
                
                if (enemy == transform) continue;
                
                var damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsDead()) continue;
                
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(enemy.position);
                if (screenPos.z < 0) continue;
                
                float screenDistance = Vector2.Distance(screenPos, screenCenter);
                
                if (screenDistance < closestDistance)
                {
                    closestDistance = screenDistance;
                    closestEnemy = enemy;
                }
            }
            
            return closestEnemy;
        }
        
        private void ReleaseLockOn()
        {
            _currentTarget = null;
            _isLockedOn = false;
            
            // 카메라 락온 해제
            if (_cameraController != null)
            {
                _cameraController.ReleaseLockOn();
            }
            
            OnLockOff?.Invoke();
            
            #if UNITY_EDITOR
            if (showDebugInfo)
                Debug.Log("락온 해제");
            #endif
        }
        
        private void UpdateLockOn()
        {
            if (_currentTarget == null)
            {
                ReleaseLockOn();
                return;
            }
            
            var damageable = _currentTarget.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsDead())
            {
                ReleaseLockOn();
                return;
            }
            
            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            if (distance > targetLostDistance)
            {
                ReleaseLockOn();
            }
        }
        
        public Vector3 GetDirectionToTarget()
        {
            if (!_isLockedOn || _currentTarget == null)
                return transform.forward;
            
            Vector3 direction = _currentTarget.position - transform.position;
            direction.y = 0;
            return direction.normalized;
        }
        
        public Quaternion GetRotationToTarget()
        {
            if (!_isLockedOn || _currentTarget == null)
                return transform.rotation;
            
            return Quaternion.LookRotation(GetDirectionToTarget());
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lockOnRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, targetLostDistance);
            
            if (_isLockedOn && _currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                Gizmos.DrawWireSphere(_currentTarget.position, 1f);
            }
        }
    }
}