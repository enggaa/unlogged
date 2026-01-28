using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.Managers;

namespace GameCore.Camera
{
    /// <summary>
    /// TPS 카메라 컨트롤러 + 락온 기능
    /// </summary>
    public class TPSCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

        [Header("Camera Rotation Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gamepadSensitivity = 100f;
        [SerializeField] private bool invertY = false;

        [Header("Camera Limits")]
        [SerializeField] private float minXRotation = -50f;
        [SerializeField] private float maxXRotation = 50f;

        [Header("Camera Distance")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField] private float defaultDistance = 3f;
        [SerializeField] private float zoomSpeed = 2f;
        
        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionOffset = 0.2f;
        
        [Header("Lock-On")]
        [SerializeField] private float lockOnSmoothness = 5f; // 락온 시 부드러운 전환
        [SerializeField] private Vector3 lockOnTargetOffset = new Vector3(0, 1f, 0); // 락온 타겟 오프셋

        private InputManager _input;
        private Transform _cameraTransform;
        private float _currentDistance;
        private float _targetDistance;
        
        private float _rotationX = 0f;
        private float _rotationY = 0f;
        
        private bool _cursorLocked = true;

        // 캐싱
        private Vector3 _desiredPosition;
        private Vector3 _cameraDirection;
        
        // 락온 시스템
        private Transform _lockOnTarget;
        private bool _isLockOnMode = false;

        private void Start()
        {
            _input = GameManager.Instance.InputManager;
            
            _cameraTransform = GetComponentInChildren<UnityEngine.Camera>().transform;
            
            if (_cameraTransform == null)
            {
                Debug.LogError("Camera not found as child!");
                return;
            }

            _currentDistance = defaultDistance;
            _targetDistance = defaultDistance;
            
            _rotationY = transform.eulerAngles.y;
            _rotationX = transform.eulerAngles.x;

            _cameraTransform.localPosition = new Vector3(0, 0, -_currentDistance);

            LockCursor();
        }

        private void Update()
        {
            HandleCursorLock();
            HandleZoom();
        }

        private void LateUpdate()
        {
            if (target == null || _cameraTransform == null) return;

            FollowTarget();
            RotateCamera();
            HandleCameraCollision();
        }

        private void FollowTarget()
        {
            _desiredPosition = target.position + targetOffset;
            transform.position = Vector3.Lerp(
                transform.position, 
                _desiredPosition, 
                followSpeed * Time.deltaTime
            );
        }

        private void RotateCamera()
        {
            // 락온 모드일 때
            if (_isLockOnMode && _lockOnTarget != null)
            {
                RotateCameraToLockOnTarget();
                return;
            }
            
            // 일반 모드
            if (!_cursorLocked) return;

            Vector2 lookInput = _input.LookInput;

            if (lookInput.x != 0f || lookInput.y != 0f)
            {
                float sensitivity = mouseSensitivity;

                if (Mathf.Abs(lookInput.x) < 1f || Mathf.Abs(lookInput.y) < 1f)
                {
                    sensitivity = gamepadSensitivity * Time.deltaTime;
                }

                _rotationY += lookInput.x * sensitivity;
                _rotationX += lookInput.y * sensitivity * (invertY ? 1f : -1f);
                _rotationX = Mathf.Clamp(_rotationX, minXRotation, maxXRotation);

                transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
            }
        }
        
        /// <summary>
        /// 락온 타겟을 바라보도록 카메라 회전
        /// </summary>
        private void RotateCameraToLockOnTarget()
        {
            if (_lockOnTarget == null)
            {
                _isLockOnMode = false;
                return;
            }
            
            // 타겟 방향 계산 (오프셋 포함)
            Vector3 targetPosition = _lockOnTarget.position + lockOnTargetOffset;
            Vector3 directionToTarget = targetPosition - transform.position;
            
            // 목표 회전
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // 부드럽게 회전
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * lockOnSmoothness
            );
            
            // 현재 회전을 내부 값에 저장 (락온 해제 시 자연스러운 전환)
            Vector3 currentEuler = transform.rotation.eulerAngles;
            _rotationX = NormalizeAngle(currentEuler.x);
            _rotationY = currentEuler.y;
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollDelta = mouse.scroll.ReadValue().y;
                if (scrollDelta != 0f)
                {
                    _targetDistance -= scrollDelta * 0.1f * zoomSpeed;
                    _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
                }
            }

            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * 10f);
        }

        private void HandleCameraCollision()
        {
            _cameraDirection = -transform.forward;
            
            Ray ray = new Ray(transform.position, _cameraDirection);
            RaycastHit hit;

            float finalDistance = _currentDistance;

            if (Physics.Raycast(ray, out hit, _currentDistance, collisionLayers))
            {
                finalDistance = Mathf.Clamp(
                    hit.distance - collisionOffset, 
                    minDistance, 
                    _currentDistance
                );
            }

            _cameraTransform.localPosition = new Vector3(0, 0, -finalDistance);
        }

        private void HandleCursorLock()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _cursorLocked = !_cursorLocked;
                if (_cursorLocked)
                    LockCursor();
                else
                    UnlockCursor();
            }

            if (!_cursorLocked && mouse.leftButton.wasPressedThisFrame)
            {
                _cursorLocked = true;
                LockCursor();
            }
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetSensitivity(float newSensitivity)
        {
            mouseSensitivity = newSensitivity;
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }
        
        /// <summary>
        /// 락온 타겟 설정
        /// </summary>
        public void SetLockOnTarget(Transform lockTarget)
        {
            _lockOnTarget = lockTarget;
            _isLockOnMode = true;
        }
        
        /// <summary>
        /// 락온 해제
        /// </summary>
        public void ReleaseLockOn()
        {
            _lockOnTarget = null;
            _isLockOnMode = false;
        }
        
        /// <summary>
        /// 각도를 -180~180 범위로 정규화
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f)
                angle -= 360f;
            while (angle < -180f)
                angle += 360f;
            return angle;
        }

        private void OnDrawGizmos()
        {
            if (target == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + targetOffset, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position - transform.forward * _currentDistance);
            
            // 락온 타겟 표시
            if (_isLockOnMode && _lockOnTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_lockOnTarget.position + lockOnTargetOffset, 0.5f);
                Gizmos.DrawLine(transform.position, _lockOnTarget.position + lockOnTargetOffset);
            }
        }
    }
}