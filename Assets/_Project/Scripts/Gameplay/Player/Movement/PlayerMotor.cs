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
        /* ----------------------------- Runtime Fields ----------------------------- */

        public MotionSourceType MotionSource;
        private bool grounded = false;
        private Vector3 speed = Vector3.zero;
        private UnityEngine.InputSystem.InputAction moveAction;

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
            InitializeInput();
        }

        private void Update()
        {
            if (player == null || charController == null || physicsData == null || worldPhysicsData == null)
            {
                return;
            }

            GravityUpdate();

            if (player.State.IsDead)
            {
                return;
            }

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

            if (player.Input.currentActionMap == null)
            {
                if (player.Input.actions != null && player.Input.actions.actionMaps.Count > 0)
                {
                    player.Input.SwitchCurrentActionMap(player.Input.actions.actionMaps[0].name);
                }
                else
                {
                    return;
                }
            }

            moveAction = player.Input.currentActionMap.FindAction("Move");
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
                var moveDir = new Vector3(input.x, 0f, input.y);
                moveDir = transform.TransformDirection(moveDir);
                var move = moveDir * (fallbackMoveSpeed * moveSpeedMultiplier) * Time.deltaTime;
                charController.Move(move);
            }
        }

        /* ----------------------------- Private Methods ---------------------------- */

        private void GravityUpdate()
        {
            UpdateGroundedState();
            if (!grounded)
            {
                Speed += worldPhysicsData.Gravity  * Time.deltaTime;
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
            var ray = new Ray(transform.position, Vector3.down);
            grounded = Physics.SphereCast(ray, charController.radius + 0.1f, charController.height / 2f + 0.5f, physicsData.GroundDetectionLayers.value);
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
        }

        /* --------------------------------- Helpers -------------------------------- */

        private float GetMovementSpeedMultiplier()
        {
            float moveSpeedMultiplier = 1f;
            bool isBlocking = player.State.IsBlocking;
            if (isBlocking)
            {
                moveSpeedMultiplier *= physicsData.BlockingMoveSpeedMultiplier;
            }
            return moveSpeedMultiplier;
        }

        private float CalculateFallDamage(float fallSpeed)
        {
            if (fallSpeed > physicsData.MinimumFallDamageSpeed)
            {
                return Mathf.CeilToInt(fallSpeed * physicsData.FallDamageMultiplier);
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

        public Vector2 GetDirectionInXZPlane()
        {
            var forward = transform.forward;
            var flattened = new Vector2(forward.x, forward.z);
            return flattened.sqrMagnitude > 0f ? flattened.normalized : Vector2.zero;
        }
        private Vector2 ReadMoveInput()
        {
            if (moveAction == null && player != null && player.Input != null && player.Input.currentActionMap != null)
            {
                moveAction = player.Input.currentActionMap.FindAction("Move");
            }

            if (moveAction != null && moveAction.enabled)
            {
                return moveAction.ReadValue<Vector2>();
            }

            return Vector2.zero;
        }

        private bool HasAnimatorController()
        {
            return player != null
                && player.Anim != null
                && player.Anim.runtimeAnimatorController != null;
        }

        /* -------------------------------------------------------------------------- */
    }
}