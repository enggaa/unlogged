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

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -15f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraRig; // CameraRig 참조! (Camera.main 대신)

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Combat")]
        [SerializeField] private float dodgeStaminaCost = 20f;
        [SerializeField] private float sprintStaminaCost = 10f;

        private CharacterController _controller;
        private InputManager _input;
        private CharacterStats _stats;
        private DamageableEntity _damageableEntity;
        
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
            
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Start()
        {
            _input = GameManager.Instance.InputManager;
            
            // CameraRig 자동 찾기 (할당되지 않았을 경우)
            if (cameraRig == null)
            {
                GameObject cameraRigObj = GameObject.Find("CameraRig");
                if (cameraRigObj != null)
                {
                    cameraRig = cameraRigObj.transform;
                    Debug.Log("CameraRig found automatically!");
                }
                else
                {
                    Debug.LogWarning("CameraRig not found! Please assign it or create a GameObject named 'CameraRig'");
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
                // ✅ CameraRig 기준으로 방향 계산!
                if (cameraRig == null)
                {
                    Debug.LogWarning("CameraRig is not assigned!");
                    return;
                }

                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg 
                                  + cameraRig.eulerAngles.y; // ← CameraRig 사용!
                
                // 부드러운 회전
                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, 
                    targetAngle, 
                    ref _rotationVelocity, 
                    0.12f
                );
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);

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

        // 카메라 방향 벡터 가져오기 (다른 스크립트에서 사용 가능)
        public Vector3 GetCameraForward()
        {
            if (cameraRig == null) return transform.forward;
            
            Vector3 forward = cameraRig.forward;
            forward.y = 0; // Y축 무시 (수평면만)
            return forward.normalized;
        }

        public Vector3 GetCameraRight()
        {
            if (cameraRig == null) return transform.right;
            
            Vector3 right = cameraRig.right;
            right.y = 0; // Y축 무시
            return right.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }

            // 이동 방향 표시
            if (Application.isPlaying && _moveDirection != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, _moveDirection * 2f);
            }
        }
    }
}