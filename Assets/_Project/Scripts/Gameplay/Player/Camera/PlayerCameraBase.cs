using UnityEngine;
using Unity.Cinemachine;

namespace BrightSouls.Gameplay
{
    public abstract class PlayerCameraBase : MonoBehaviour
    {
        public bool IsLockOnCamera { get => this is LockOnCamera; }
        public bool IsThirdPersonCamera { get => this is ThirdPersonCamera; }

        public abstract CinemachineCamera CinemachineCamera { get; }
        public abstract void SetPriority(int value);
    }
}
