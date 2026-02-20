using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrightSouls.Gameplay
{
    [RequireComponent(typeof(Player))]
    public class PlayerMotor : MonoBehaviour
    {
        /* ---------------------------- Type Definitions ---------------------------- */

        public enum MotionSourceType
        {
            Motor,
            Animation
        }

        /* ------------------------------- Properties ------------------------------- */

        public MoveCommand Move
        {
            get;
            private set;
        }

        // * Speed:
        // *   Only used for vertical movement caused by gravity.
        // *   Ground movement is handled by the Player's Animator.
        private Vector3 Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
                if (HasAnimatorController())
                {
                    player.Anim.SetFloat("speed_y", speed.y);
                }
            }
        }

        /* ------------------------ Inspector-Assigned Fields ----------------------- */

        [Header("Component Refs")]
        [SerializeField] private Player player;
        [SerializeField] private CharacterController charController;

        [Header("Physics Data")]
        [SerializeField] private PlayerPhysicsData physicsData;
        [SerializeField] private WorldPhysicsData  worldPhysicsData;

        [Header("Fallback Movement")]
        [SerializeField] private float fallbackMoveSpeed = 4f;
        [SerializeField] private float sprintSpeedMultiplier = 1.5f;
        [SerializeField] private float jumpVelocity = 6f;
        [SerializeField] private float fallbackAcceleration = 18f;
        [SerializeField] private float fallbackDeceleration = 24f;
        [SerializeField] private float coyoteTime = 0.12f;

        [Header("Fallback Physics")]
        [SerializeField] private Vector3 fallbackGravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField] private float fallbackBlockingMoveSpeedMultiplier = 0.5f;

        [Header("Ground Probe")]
        [SerializeField] private float groundedProbeOffset = 0.08f;
        [SerializeField] private float groundedProbeRadiusScale = 0.9f;

        [Header("Debug")]
        [SerializeField] private bool verboseActionLogging = true;
        /* ----------------------------- Runtime Fields ----------------------------- */

        public MotionSourceType MotionSource;
        private bool grounded = false;
        private Vector3 speed = Vector3.zero;
        private UnityEngine.InputSystem.InputAction moveAction;
        private UnityEngine.InputSystem.InputAction jumpAction;
        private UnityEngine.InputSystem.InputAction sprintAction;
        private bool sprintHeld;
        private bool hasWarnedMissingPhysicsData;
        private bool hasWarnedMissingGroundMask;
        private float lastGroundedTime;
        private Vector3 fallbackPlanarVelocity;
        private bool wasMovingLastFrame;
        private bool wasGroundedLastFrame;

        /* ------------------------------ Unity Events ------------------------------ */

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }

            if (player == null)
            {
                player = GetComponentInParent<Player>();
            }

            if (player == null)
            {
                player = GetComponentInChildren<Player>();
            }

            if (charController == null)
            {
                charController = GetComponent<CharacterController>();
            }

            if (charController == null)
            {
                charController = GetComponentInParent<CharacterController>();
            }

            if (charController == null)
            {
                charController = GetComponentInChildren<CharacterController>();
            }
        }

        private void Start()
        {
            InitializeCommands();
            EnsurePhysicsDataReady();
            InitializeInput();
        }

        private void Update()
        {
            if (player == null || charController == null)
            {
                return;
            }

            GravityUpdate();

            if (player.State.IsDead)
            {
                return;
            }

            UpdateActionStates();

            var moveInput = ReadMoveInput();
            Move.Execute(moveInput);
            charController.Move(Speed * Time.deltaTime);
        }

        /* ----------------------------- Initialization ----------------------------- */

        private void InitializeCommands()
        {
            if (player == null)
            {
                Debug.LogError("PlayerMotor requires a Player reference.");
                return;
            }

            Move = new MoveCommand(this.player);
        }

        private void InitializeInput()
        {
            if (player == null || player.Input == null)
            {
                return;
            }

            if (!player.EnsureInputReady())
            {
                Debug.LogWarning("PlayerMotor could not initialize player input.");
                return;
            }

            moveAction = player.Input.currentActionMap.FindAction("Move");
            jumpAction = player.Input.currentActionMap.FindAction("Jump");
            sprintAction = player.Input.currentActionMap.FindAction("Sprint");
        }

        private void EnsurePhysicsDataReady()
        {
            if (physicsData != null && worldPhysicsData != null)
            {
                return;
            }

            if (!hasWarnedMissingPhysicsData)
            {
                Debug.LogWarning("PlayerMotor is missing Physics Data references. Falling back to serialized defaults.");
                hasWarnedMissingPhysicsData = true;
            }
        }

        /* ----------------------------- Public Methods ----------------------------- */

        public void PerformGroundMovement(Vector2 input)
        {
            if (player == null || player.Anim == null)
            {
                return;
            }

            input = ClampMovementInput(input);
            var moveSpeedMultiplier = GetMovementSpeedMultiplier();
            if (HasAnimatorController())
            {
                // Actual transform movement is handled by the animator
                player.Anim.SetFloat("move_speed", input.magnitude);
                player.Anim.SetFloat("move_x", input.x * moveSpeedMultiplier);
                player.Anim.SetFloat("move_y", input.y * moveSpeedMultiplier);
            }
            else if (charController != null)
            {
                var desiredDir = new Vector3(input.x, 0f, input.y);
                desiredDir = transform.TransformDirection(desiredDir);
                var desiredVelocity = desiredDir * (fallbackMoveSpeed * moveSpeedMultiplier);

                bool hasInput = desiredVelocity.sqrMagnitude > 0.0001f;
                float accel = hasInput ? fallbackAcceleration : fallbackDeceleration;
                fallbackPlanarVelocity = Vector3.MoveTowards(fallbackPlanarVelocity, desiredVelocity, accel * Time.deltaTime);

                var move = fallbackPlanarVelocity * Time.deltaTime;
                charController.Move(move);
            }
        }

        /* ----------------------------- Private Methods ---------------------------- */

        private void GravityUpdate()
        {
            UpdateGroundedState();
            if (!grounded)
            {
                Vector3 gravity = worldPhysicsData != null ? worldPhysicsData.Gravity : fallbackGravity;
                Speed += gravity * Time.deltaTime;
            }
            else
            {
                bool wasFalling = Speed.y < 0f;
                if (wasFalling)
                {
                    OnHitGround();
                }
            }
        }

        private void UpdateGroundedState()
        {
            grounded = EvaluateGroundedState();
            if (grounded)
            {
                lastGroundedTime = Time.time;
            }

            if (verboseActionLogging && grounded != wasGroundedLastFrame)
            {
                Debug.Log($"[PlayerMotor] Grounded state changed: {wasGroundedLastFrame} -> {grounded}");
                wasGroundedLastFrame = grounded;
            }

            if (HasAnimatorController())
            {
                player.Anim.SetBool("grounded", grounded);
                // Animator also applies gravity, so when not grounded disable animator physics
                player.Anim.applyRootMotion = grounded;
            }
        }

        private void OnHitGround()
        {
            float fallSpeed = Mathf.Abs(Speed.y);
            float fallDamage = CalculateFallDamage(fallSpeed);
            player.Attributes.Health.Value -= fallDamage;
            // Reset the vertical speed when hitting ground
            Speed = new Vector3(Speed.x, 0f, Speed.z);
            // Teleport player vertically to avoid getting stuck in the ground when falling at high speeds
            charController.Move(new Vector3(0f, -0.5f, 0f));

            if (player.State != null && player.State.Fsm != null && player.State.IsJumping)
            {
                player.State.Fsm.TrySetState<PlayerStateDefault>();
            }
        }

        /* --------------------------------- Helpers -------------------------------- */

        private float GetMovementSpeedMultiplier()
        {
            float moveSpeedMultiplier = 1f;
            bool isBlocking = player.State.IsBlocking;
            if (isBlocking)
            {
                moveSpeedMultiplier *= physicsData != null
                    ? physicsData.BlockingMoveSpeedMultiplier
                    : fallbackBlockingMoveSpeedMultiplier;
            }

            if (sprintHeld)
            {
                moveSpeedMultiplier *= sprintSpeedMultiplier;
            }

            return moveSpeedMultiplier;
        }

        private float CalculateFallDamage(float fallSpeed)
        {
            float minimumFallDamageSpeed = physicsData != null ? physicsData.MinimumFallDamageSpeed : float.MaxValue;
            float fallDamageMultiplier = physicsData != null ? physicsData.FallDamageMultiplier : 0f;

            if (fallSpeed > minimumFallDamageSpeed)
            {
                return Mathf.CeilToInt(fallSpeed * fallDamageMultiplier);
            }
            else
            {
                return 0f;
            }
        }

        private Vector2 ClampMovementInput(Vector2 input)
        {
            if (input.magnitude > 1f)
            {
                return input.normalized;
            }
            else
            {
                // 데드존 처리: 절댓값이 0.1 미만이면 0으로
                if (Mathf.Abs(input.x) < 0.1f)
                {
                    input.x = 0f;
                }
                if (Mathf.Abs(input.y) < 0.1f)
                {
                    input.y = 0f;
                }
                return input;
            }
        }

        private void UpdateActionStates()
        {
            EnsureInputActionsCached();

            sprintHeld = sprintAction != null && sprintAction.enabled && sprintAction.IsPressed();

            bool jumpPressed = jumpAction != null && jumpAction.enabled && jumpAction.WasPressedThisFrame();
            bool canJump = grounded || (Time.time - lastGroundedTime) <= coyoteTime;
            bool shouldJump = jumpPressed && canJump;

            if (jumpPressed && verboseActionLogging)
            {
                Debug.Log($"[PlayerMotor] Jump requested. grounded={grounded}, canJump={canJump}, coyoteWindow={(Time.time - lastGroundedTime):0.000}s");
                if (!shouldJump)
                {
                    Debug.Log("[PlayerMotor] Jump rejected: character is not grounded and coyote time has expired.");
                }
            }

            if (shouldJump)
            {
                grounded = false;
                Speed = new Vector3(Speed.x, jumpVelocity, Speed.z);
                if (player.State != null && player.State.Fsm != null)
                {
                    bool changedState = player.State.Fsm.TrySetState<PlayerStateJumping>();
                    if (verboseActionLogging)
                    {
                        Debug.Log($"[PlayerMotor] Jump accepted. verticalSpeed={Speed.y:0.00}, stateChanged={changedState}");
                    }
                }
                else if (verboseActionLogging)
                {
                    Debug.Log($"[PlayerMotor] Jump accepted. verticalSpeed={Speed.y:0.00}, but PlayerState FSM is missing.");
                }

                if (HasAnimatorController())
                {
                    player.Anim.SetTrigger("jump");
                }
            }

            LogMoveInputIfChanged();
        }

        public Vector2 GetDirectionInXZPlane()
        {
            var forward = transform.forward;
            var flattened = new Vector2(forward.x, forward.z);
            return flattened.sqrMagnitude > 0f ? flattened.normalized : Vector2.zero;
        }
        private Vector2 ReadMoveInput()
        {
            EnsureInputActionsCached();

            if (moveAction != null && moveAction.enabled)
            {
                return moveAction.ReadValue<Vector2>();
            }

            return Vector2.zero;
        }

        private void EnsureInputActionsCached()
        {
            if (player == null || player.Input == null || player.Input.currentActionMap == null)
            {
                return;
            }

            if (moveAction == null)
            {
                moveAction = player.Input.currentActionMap.FindAction("Move");
            }

            if (jumpAction == null)
            {
                jumpAction = player.Input.currentActionMap.FindAction("Jump");
            }

            if (sprintAction == null)
            {
                sprintAction = player.Input.currentActionMap.FindAction("Sprint");
            }
        }

        private bool HasAnimatorController()
        {
            return player != null
                && player.Anim != null
                && player.Anim.runtimeAnimatorController != null;
        }

        private bool EvaluateGroundedState()
        {
            if (charController == null)
            {
                return false;
            }

            bool controllerGrounded = charController.isGrounded;
            if (controllerGrounded)
            {
                return true;
            }

            if (physicsData == null)
            {
                return controllerGrounded;
            }

            int layerMask = physicsData.GroundDetectionLayers.value;
            if (layerMask == 0)
            {
                if (!hasWarnedMissingGroundMask)
                {
                    Debug.LogWarning("[PlayerMotor] GroundDetectionLayers is empty. Falling back to CharacterController.isGrounded.");
                    hasWarnedMissingGroundMask = true;
                }

                return controllerGrounded;
            }

            float probeRadius = Mathf.Max(0.01f, charController.radius * groundedProbeRadiusScale);
            float feetOffset = (charController.height * 0.5f) - charController.radius;
            Vector3 probeOrigin = transform.position + charController.center + Vector3.down * feetOffset;
            Vector3 checkCenter = probeOrigin + (Vector3.up * groundedProbeOffset);

            bool overlapGrounded = Physics.CheckSphere(checkCenter, probeRadius, layerMask, QueryTriggerInteraction.Ignore);
            if (overlapGrounded)
            {
                return true;
            }

            var ray = new Ray(checkCenter + Vector3.up * 0.02f, Vector3.down);
            float castDistance = groundedProbeOffset + 0.08f;
            bool castGrounded = Physics.SphereCast(ray, probeRadius, castDistance, layerMask, QueryTriggerInteraction.Ignore);
            return castGrounded;
        }

        private void LogMoveInputIfChanged()
        {
            if (!verboseActionLogging || moveAction == null || !moveAction.enabled)
            {
                return;
            }

            bool movingNow = moveAction.ReadValue<Vector2>().sqrMagnitude > 0.001f;
            if (movingNow == wasMovingLastFrame)
            {
                return;
            }

            wasMovingLastFrame = movingNow;
            Debug.Log(movingNow ? "[PlayerMotor] Move started." : "[PlayerMotor] Move stopped.");
        }

        /* -------------------------------------------------------------------------- */
    }
}
