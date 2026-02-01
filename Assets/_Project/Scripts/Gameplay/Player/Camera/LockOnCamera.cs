using UnityEngine;
using Unity.Cinemachine;

namespace BrightSouls.Gameplay
{
    public sealed class LockOnCamera : PlayerCameraBase
    {
        public override CinemachineCamera CinemachineCamera { get => virtualCamera; }
        public ChangeTargetCommand ChangeTarget { get; private set; }
        public ICharacter Target { get => lockOnTarget; }

        public LockOnCommand lockOn;

        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private Player player;

        private ICharacter lockOnTarget;
        private LockOnDetector lockOnDetector;

        public override void SetPriority(int value)
        {
            virtualCamera.Priority = value;
        }

        private void Start()
        {
            ChangeTarget = new ChangeTargetCommand(player);
            lockOnDetector = player.GetComponentInChildren<LockOnDetector>();
        }

        public class LockOnCommand : PlayerCommand
        {
            public LockOnCommand(Gameplay.Player player) : base(player) { }

            public override bool CanExecute()
            {
                return true;
            }

            public override void Execute()
            {
            }
        }

        public class ChangeTargetCommand : PlayerCommand<Vector2>
        {
            public ChangeTargetCommand(Gameplay.Player player) : base(player) { }

            public override bool CanExecute()
            {
                return true;
            }

            public override void Execute(Vector2 input)
            {
            }
        }
    }
}
