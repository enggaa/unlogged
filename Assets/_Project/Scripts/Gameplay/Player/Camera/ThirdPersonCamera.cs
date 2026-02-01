using UnityEngine;
using Unity.Cinemachine;

namespace BrightSouls.Gameplay
{
    /// <summary>
    /// Cinemachine 3.x 기반 ThirdPerson Orbit 카메라.
    /// CinemachineCamera는 Priority / Blending만 담당.
    /// 실제 오빗 회전·위치는 LateUpdate에서 직접 제어.
    ///
    /// ⚠️ Inspector에서 freeLookCamera의 Follow / Aim 확장은 추가하지 마세요.
    ///    추가하면 Cinemachine이 Transform을 덮어씁니다.
    /// </summary>
    public sealed class ThirdPersonCamera : PlayerCameraBase
    {
        /* ------------------------------- Definitions ------------------------------ */

        public sealed class RotateCameraCommand : PlayerCommand<Vector2>
        {
            private ThirdPersonCamera thirdPersonCamera = null;

            public RotateCameraCommand(Player player) : base(player)
            {
                thirdPersonCamera = player.CameraDirector.GetCamera<ThirdPersonCamera>();
            }

            public override bool CanExecute()
            {
                return true;
            }

            public override void Execute(Vector2 input)
            {
                thirdPersonCamera.SetInputAxisValue(input);
            }
        }

        /* ------------------------------- Properties ------------------------------- */

        public override CinemachineCamera CinemachineCamera { get => freeLookCamera; }

        public RotateCameraCommand Look { get; private set; }

        /* ------------------------ Inspector-assigned Fields ----------------------- */

        [SerializeField] private Gameplay.Player player;
        [SerializeField] private CinemachineCamera freeLookCamera;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitDistance = 5f;
        [SerializeField] private float orbitSpeedX = 200f;
        [SerializeField] private float orbitSpeedY = 150f;
        [SerializeField] private float minPitchAngle = -30f;
        [SerializeField] private float maxPitchAngle = 60f;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        /* ----------------------------- Runtime Fields ----------------------------- */

        private float _yaw = 0f;
        private float _pitch = 0f;
        private Vector2 _inputAxis = Vector2.zero;

        /* ------------------------------ Unity Events ------------------------------ */

        private void Start()
        {
            InitializeCommands();
            InitializeInput();

            // 초기 회전값을 카메라 현재 회전으로 동기화
            _yaw   = freeLookCamera.transform.eulerAngles.y;
            _pitch = freeLookCamera.transform.eulerAngles.x;
        }

        private void LateUpdate()
        {
            if (player == null || freeLookCamera == null) return;

            // 회전 누적
            _yaw   +=  _inputAxis.x * orbitSpeedX * Time.deltaTime;
            _pitch -= _inputAxis.y * orbitSpeedY * Time.deltaTime;
            _pitch  = Mathf.Clamp(_pitch, minPitchAngle, maxPitchAngle);

            // 타겟 중심점 (플레이어 머리 높이)
            Vector3 targetPos = player.transform.position + targetOffset;

            // 오빗 회전·방향 계산
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 direction  = rotation * Vector3.back;

            // CinemachineCamera Transform 직접 제어
            freeLookCamera.transform.position = targetPos + direction * orbitDistance;
            freeLookCamera.transform.rotation = rotation;
        }

        /* ----------------------------- Initialization ----------------------------- */

        private void InitializeCommands()
        {
            Look = new RotateCameraCommand(player);
        }

        private void InitializeInput()
        {
            var look = player.Input.currentActionMap.FindAction("Look");
            look.performed += ctx => Look.Execute(look.ReadValue<Vector2>());
        }

        /* --------------------------- Core Functionality --------------------------- */

        public override void SetPriority(int value)
        {
            freeLookCamera.Priority = value;
        }

        /// <summary>
        /// 프레임당 오빗 입력축 값을 세팅. (RotateCameraCommand → 호출)
        /// </summary>
        public void SetInputAxisValue(Vector2 input)
        {
            _inputAxis = input;
        }

        /// <summary>
        /// 오빗 회전 속도 변경 (기존 SetMaxSpeed 호환)
        /// </summary>
        public void SetMaxSpeed(float x, float y)
        {
            orbitSpeedX = x;
            orbitSpeedY = y;
        }

        /* -------------------------------------------------------------------------- */
    }
}
