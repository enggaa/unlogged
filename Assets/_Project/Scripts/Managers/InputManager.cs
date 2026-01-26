using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore.Managers
{
    public class InputManager : MonoBehaviour
    {
        private PlayerInputActions _inputActions;
        
        // Properties with getters only (최적화)
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool LockOnPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool HeavyAttackPressed { get; private set; }

        #if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false; // 기본값 false로 변경
        #endif

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            
            // 이벤트 구독
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Look.canceled += OnLook;
            
            _inputActions.Player.Jump.performed += OnJump;
            _inputActions.Player.Sprint.performed += OnSprint;
            _inputActions.Player.Sprint.canceled += OnSprint;
            _inputActions.Player.Dodge.performed += OnDodge;
            _inputActions.Player.LockOn.performed += OnLockOn;
            _inputActions.Player.Attack.performed += OnAttack;
            _inputActions.Player.HeavyAttack.performed += OnHeavyAttack;
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Look.canceled -= OnLook;
            
            _inputActions.Player.Jump.performed -= OnJump;
            _inputActions.Player.Sprint.performed -= OnSprint;
            _inputActions.Player.Sprint.canceled -= OnSprint;
            _inputActions.Player.Dodge.performed -= OnDodge;
            _inputActions.Player.LockOn.performed -= OnLockOn;
            _inputActions.Player.Attack.performed -= OnAttack;
            _inputActions.Player.HeavyAttack.performed -= OnHeavyAttack;
            
            _inputActions.Player.Disable();
        }

        // 입력 콜백 함수들
        private void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
            
            #if UNITY_EDITOR
            // 최적화: 에디터에서만 디버그 로그, showDebugLogs가 true일 때만
            if (showDebugLogs && LookInput.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"Look Input: {LookInput} (Magnitude: {LookInput.magnitude})");
            }
            #endif
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            JumpPressed = true;
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            SprintHeld = context.performed;
        }

        private void OnDodge(InputAction.CallbackContext context)
        {
            DodgePressed = true;
        }

        private void OnLockOn(InputAction.CallbackContext context)
        {
            LockOnPressed = true;
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            AttackPressed = true;
        }

        private void OnHeavyAttack(InputAction.CallbackContext context)
        {
            HeavyAttackPressed = true;
        }

        private void LateUpdate()
        {
            // 버튼 입력 초기화 (한 프레임만 유효)
            JumpPressed = false;
            DodgePressed = false;
            LockOnPressed = false;
            AttackPressed = false;
            HeavyAttackPressed = false;
        }

        public void EnableInput()
        {
            _inputActions?.Player.Enable();
        }

        public void DisableInput()
        {
            _inputActions?.Player.Disable();
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지
            _inputActions?.Dispose();
        }
    }
}