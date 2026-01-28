using UnityEngine;
using GameCore.Managers;

namespace GameCore.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float lockOnRotationSpeed = 8f; // 락온 시 회전 속도

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -15f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraRig;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Combat")]
        [SerializeField] private float dodgeStaminaCost = 20f;
        [SerializeField] private float sprintStaminaCost = 10f;

        private CharacterController _controller;
        private InputManager _input;
        private CharacterStats _stats;
        private DamageableEntity _damageableEntity;
        private LockOnSystem _lockOnSystem; // 락온 시스템 참조 추가
        
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private float _currentSpeed;
        private bool _isGrounded;
        private float _targetRotation;
        private float _rotationVelocity;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stats = GetComponent<CharacterStats>();
            _damageableEntity = GetComponent<DamageableEntity>();
            _lockOnSystem = GetComponent<LockOnSystem>(); // 락온 시스템 가져오기
            
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Start()
        {
            _input = GameManager.Instance.InputManager;
            
            // CameraRig 자동 찾기
            if (cameraRig == null)
            {
                GameObject cameraRigObj = GameObject.Find("CameraRig");
                if (cameraRigObj != null)
                {
                    cameraRig = cameraRigObj.transform;
                }
                else
                {
                    Debug.LogWarning("CameraRig not found!");
                }
            }

            // GroundCheck 생성
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, 0.1f, 0);
                groundCheck = groundCheckObj.transform;
            }
        }

        private void Update()
        {
            CheckGround();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            UpdateAnimations();
        }

        private void CheckGround()
        {
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            Vector2 input = _input.MoveInput;
            
            if (input.magnitude > 0.1f)
            {
                if (cameraRig == null)
                {
                    Debug.LogWarning("CameraRig is not assigned!");
                    return;
                }

                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg 
                                  + cameraRig.eulerAngles.y;
                
                // 락온 중이 아닐 때만 플레이어 회전
                if (_lockOnSystem == null || !_lockOnSystem.IsLockedOn)
                {
                    // 일반 모드: 부드러운 회전
                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y, 
                        targetAngle, 
                        ref _rotationVelocity, 
                        0.12f
                    );
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                }
                else
                {
                    // 락온 모드: 타겟 방향으로 회전하면서 이동
                    Quaternion targetRotation = _lockOnSystem.GetRotationToTarget();
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        lockOnRotationSpeed * Time.deltaTime
                    );
                }

                // 이동 방향
                _moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                // 스프린트 체크
                bool canSprint = _input.SprintHeld && _stats.HasStamina(sprintStaminaCost * Time.deltaTime);
                float targetSpeed = canSprint ? sprintSpeed : walkSpeed;
                
                if (canSprint)
                {
                    _stats.UseStamina(sprintStaminaCost * Time.deltaTime);
                }
                
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

                // 이동
                _controller.Move(_moveDirection.normalized * _currentSpeed * Time.deltaTime);
            }
            else
            {
                // 정지 중일 때도 락온 중이면 타겟 바라보기
                if (_lockOnSystem != null && _lockOnSystem.IsLockedOn)
                {
                    Quaternion targetRotation = _lockOnSystem.GetRotationToTarget();
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        lockOnRotationSpeed * Time.deltaTime
                    );
                }
                
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, acceleration * Time.deltaTime);
            }
        }

        private void HandleJump()
        {
            if (_input.JumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                if (animator != null)
                {
                    animator.SetTrigger(JumpHash);
                }
            }
        }

        private void ApplyGravity()
        {
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void UpdateAnimations()
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, _currentSpeed);
                animator.SetBool(IsGroundedHash, _isGrounded);
            }
        }

        public Vector3 GetCameraForward()
        {
            if (cameraRig == null) return transform.forward;
            
            Vector3 forward = cameraRig.forward;
            forward.y = 0;
            return forward.normalized;
        }

        public Vector3 GetCameraRight()
        {
            if (cameraRig == null) return transform.right;
            
            Vector3 right = cameraRig.right;
            right.y = 0;
            return right.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }

            if (Application.isPlaying && _moveDirection != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, _moveDirection * 2f);
            }
            
            // 락온 중일 때 타겟 방향 표시
            if (_lockOnSystem != null && _lockOnSystem.IsLockedOn)
            {
                Gizmos.color = Color.red;
                Vector3 targetDir = _lockOnSystem.GetDirectionToTarget();
                Gizmos.DrawRay(transform.position + Vector3.up, targetDir * 2f);
            }
        }
    }
}