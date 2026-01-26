using UnityEngine;
using UnityEngine.InputSystem;
using GameCore.Managers;

namespace GameCore.Camera
{
    /// <summary>
    /// TPS 카메라 컨트롤러
    /// 이 스크립트는 CameraRig (빈 GameObject)에 부착해야 합니다.
    /// 실제 카메라는 이 GameObject의 자식으로 배치합니다.
    /// </summary>
    public class TPSCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target; // 플레이어 Transform
        [SerializeField] private float followSpeed = 10f; // 부드러운 추적 속도
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0); // 플레이어 중심점 오프셋 (머리 높이)

        [Header("Camera Rotation Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gamepadSensitivity = 100f;
        [SerializeField] private bool invertY = false;

        [Header("Camera Limits")]
        [SerializeField] private float minXRotation = -50f; // 위쪽 제한
        [SerializeField] private float maxXRotation = 50f;  // 아래쪽 제한

        [Header("Camera Distance")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField] private float defaultDistance = 3f;
        [SerializeField] private float zoomSpeed = 2f;
        
        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionOffset = 0.2f;

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

        private void Start()
        {
            _input = GameManager.Instance.InputManager;
            
            // 자식 카메라 찾기
            _cameraTransform = GetComponentInChildren<UnityEngine.Camera>().transform;
            
            if (_cameraTransform == null)
            {
                Debug.LogError("Camera not found as child! Please add a Camera as child of this GameObject.");
                return;
            }

            // 초기 거리 설정
            _currentDistance = defaultDistance;
            _targetDistance = defaultDistance;
            
            // 초기 회전값 설정
            _rotationY = transform.eulerAngles.y;
            _rotationX = transform.eulerAngles.x;

            // 초기 카메라 위치 설정
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
            // 타겟 위치 + 오프셋으로 이동
            _desiredPosition = target.position + targetOffset;
            transform.position = Vector3.Lerp(
                transform.position, 
                _desiredPosition, 
                followSpeed * Time.deltaTime
            );
        }

        private void RotateCamera()
        {
            if (!_cursorLocked) return;

            Vector2 lookInput = _input.LookInput;

            if (lookInput.x != 0f || lookInput.y != 0f)
            {
                float sensitivity = mouseSensitivity;

                // 게임패드 감지 (값이 작으면 게임패드)
                if (Mathf.Abs(lookInput.x) < 1f || Mathf.Abs(lookInput.y) < 1f)
                {
                    sensitivity = gamepadSensitivity * Time.deltaTime;
                }

                // Y축 회전 (좌우)
                _rotationY += lookInput.x * sensitivity;

                // X축 회전 (상하)
                _rotationX += lookInput.y * sensitivity * (invertY ? 1f : -1f);
                _rotationX = Mathf.Clamp(_rotationX, minXRotation, maxXRotation);

                // Rig 회전 적용
                transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
            }
        }

        private void HandleZoom()
        {
            // 마우스 휠 입력 (New Input System에서는 InputManager에 추가 필요)
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

            // 부드러운 줌
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * 10f);
        }

        private void HandleCameraCollision()
        {
            // 카메라 방향 계산
            _cameraDirection = -transform.forward;
            
            // Raycast로 장애물 감지
            Ray ray = new Ray(transform.position, _cameraDirection);
            RaycastHit hit;

            float finalDistance = _currentDistance;

            if (Physics.Raycast(ray, out hit, _currentDistance, collisionLayers))
            {
                // 충돌 지점까지의 거리 계산 (약간의 오프셋 추가)
                finalDistance = Mathf.Clamp(
                    hit.distance - collisionOffset, 
                    minDistance, 
                    _currentDistance
                );
            }

            // 카메라 로컬 포지션 설정
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

        private void OnDrawGizmos()
        {
            if (target == null) return;

            // 타겟 포지션
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + targetOffset, 0.2f);

            // 카메라 충돌 체크 레이
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position - transform.forward * _currentDistance);
        }
    }
}