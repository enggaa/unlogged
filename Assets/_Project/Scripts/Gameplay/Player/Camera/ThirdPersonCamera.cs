using UnityEngine;
using Unity.Cinemachine;

namespace BrightSouls.Gameplay
{
    /// <summary>
    /// Third-person orbit camera.
    /// Supports both CinemachineCamera and plain Camera transforms.
    /// </summary>
    public sealed class ThirdPersonCamera : PlayerCameraBase
    {
        public sealed class RotateCameraCommand : PlayerCommand<Vector2>
        {
            private readonly ThirdPersonCamera thirdPersonCamera;

            public RotateCameraCommand(Player player) : base(player)
            {
                thirdPersonCamera = player.CameraDirector.GetCamera<ThirdPersonCamera>();
            }

            public override bool CanExecute()
            {
                return thirdPersonCamera != null;
            }

            public override void Execute(Vector2 input)
            {
                if (thirdPersonCamera != null)
                {
                    thirdPersonCamera.SetInputAxisValue(input);
                }
            }
        }

        public override CinemachineCamera CinemachineCamera { get => freeLookCamera; }
        public RotateCameraCommand Look { get; private set; }

        [SerializeField] private Gameplay.Player player;

        [Header("Camera Source (either one)")]
        [SerializeField] private CinemachineCamera freeLookCamera;
        [SerializeField] private Camera fallbackCamera;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitDistance = 5f;
        [SerializeField] private float orbitSpeedX = 200f;
        [SerializeField] private float orbitSpeedY = 150f;
        [SerializeField] private float minPitchAngle = -30f;
        [SerializeField] private float maxPitchAngle = 60f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        private float _yaw;
        private float _pitch;
        private Vector2 _inputAxis;
        private UnityEngine.InputSystem.InputAction lookAction;

        private Transform ActiveCameraTransform
        {
            get
            {
                if (freeLookCamera != null)
                {
                    return freeLookCamera.transform;
                }

                if (fallbackCamera != null)
                {
                    return fallbackCamera.transform;
                }

                return null;
            }
        }

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponentInParent<Player>();
            }

            if (fallbackCamera == null)
            {
                fallbackCamera = GetComponent<Camera>();
            }

            if (fallbackCamera == null && Camera.main != null)
            {
                fallbackCamera = Camera.main;
            }
        }

        private void Start()
        {
            InitializeCommands();
            InitializeInput();

            var cameraTransform = ActiveCameraTransform;
            if (cameraTransform != null)
            {
                _yaw = cameraTransform.eulerAngles.y;
                _pitch = cameraTransform.eulerAngles.x;
            }
        }

        private void LateUpdate()
        {
            var cameraTransform = ActiveCameraTransform;
            if (player == null || cameraTransform == null)
            {
                return;
            }

            _inputAxis = ReadLookInput();

            _yaw += _inputAxis.x * orbitSpeedX * Time.deltaTime;
            _pitch -= _inputAxis.y * orbitSpeedY * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, minPitchAngle, maxPitchAngle);

            Vector3 targetPos = player.transform.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 direction = rotation * Vector3.back;

            cameraTransform.position = targetPos + direction * orbitDistance;
            cameraTransform.rotation = rotation;
        }

        private void InitializeCommands()
        {
            if (player == null || player.CameraDirector == null)
            {
                return;
            }

            Look = new RotateCameraCommand(player);
        }

        private void InitializeInput()
        {
            if (player == null || player.Input == null)
            {
                Debug.LogWarning("ThirdPersonCamera requires Player input.");
                return;
            }

            if (!player.EnsureInputReady())
            {
                Debug.LogWarning("ThirdPersonCamera could not initialize player input.");
                return;
            }

            lookAction = player.Input.currentActionMap.FindAction("Look");
            if (lookAction == null)
            {
                Debug.LogWarning("ThirdPersonCamera could not find Look action.");
            }
        }

        public override void SetPriority(int value)
        {
            if (freeLookCamera != null)
            {
                freeLookCamera.Priority = value;
            }
        }

        public void SetInputAxisValue(Vector2 input)
        {
            _inputAxis = input;
        }

        public void SetMaxSpeed(float x, float y)
        {
            orbitSpeedX = x;
            orbitSpeedY = y;
        }

        private Vector2 ReadLookInput()
        {
            if (player != null && player.Input != null && player.Input.currentActionMap != null && lookAction == null)
            {
                lookAction = player.Input.currentActionMap.FindAction("Look");
            }

            if (lookAction != null && lookAction.enabled)
            {
                return lookAction.ReadValue<Vector2>();
            }

            return _inputAxis;
        }
    }
}
