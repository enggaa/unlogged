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
                if (player != null && player.Anim != null)
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

        /* ----------------------------- Runtime Fields ----------------------------- */

        public MotionSourceType MotionSource;
        private bool grounded = false;
        private Vector3 speed = Vector3.zero;

        /* ------------------------------ Unity Events ------------------------------ */

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
                Debug.LogError("PlayerMotor requires Player input.");
                return;
            }

            if (player.Input.currentActionMap == null)
            {
                Debug.LogError("PlayerMotor could not find a current action map.");
                return;
            }

            var move = player.Input.currentActionMap.FindAction("Move");
            if (move == null)
            {
                Debug.LogError("PlayerMotor could not find Move action.");
                return;
            }

            move.performed += ctx => Move.Execute(move.ReadValue<Vector2>());
            move.canceled += ctx => Move.Execute(Vector2.zero);
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
            // Actual transform movement is handled by the animator
            player.Anim.SetFloat("move_speed", input.magnitude);
            player.Anim.SetFloat("move_x", input.x * moveSpeedMultiplier);
            player.Anim.SetFloat("move_y", input.y * moveSpeedMultiplier);
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
            if (player != null && player.Anim != null)
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

        /* -------------------------------------------------------------------------- */
    }
}