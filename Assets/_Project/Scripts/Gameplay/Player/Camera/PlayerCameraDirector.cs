using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrightSouls.Gameplay
{
    public sealed class PlayerCameraDirector : MonoBehaviour
    {
        public PlayerCameraBase CurrentCamera { get => currentCamera; }

        private PlayerCameraBase currentCamera;
        private Dictionary<System.Type, PlayerCameraBase> cameraMap;

        [SerializeField] private List<PlayerCameraBase> playerCameras;

        private void Awake()
        {
            cameraMap = new Dictionary<System.Type, PlayerCameraBase>();
            RegisterConfiguredCameras();
            RegisterChildCamerasIfNeeded();
            DeactivateCameras();
        }

        private void Start()
        {
            UnparentCameras();
            ActivateCameras();
        }

        public void RegisterCamera<T>(T camera) where T : PlayerCameraBase
        {
            if (camera == null)
            {
                return;
            }

            cameraMap[typeof(T)] = camera;
        }

        public T GetCamera<T>() where T : PlayerCameraBase
        {
            if (cameraMap.TryGetValue(typeof(T), out var cam))
            {
                return cam.GetComponent<T>();
            }

            Debug.LogWarning($"GetCamera<{typeof(T).Name}>: 등록된 카메라가 없습니다.");
            return null;
        }

        private void RegisterConfiguredCameras()
        {
            if (playerCameras == null)
            {
                playerCameras = new List<PlayerCameraBase>();
                return;
            }

            foreach (var cam in playerCameras)
            {
                TryAddCamera(cam);
            }
        }

        private void RegisterChildCamerasIfNeeded()
        {
            if (cameraMap.Count > 0)
            {
                return;
            }

            var discovered = GetComponentsInChildren<PlayerCameraBase>(true);
            foreach (var cam in discovered)
            {
                TryAddCamera(cam);
            }

            if (cameraMap.Count == 0)
            {
                Debug.LogWarning("PlayerCameraDirector: No PlayerCameraBase found. Add ThirdPersonCamera to your camera object.");
            }
        }

        private void TryAddCamera(PlayerCameraBase cam)
        {
            if (cam == null || cameraMap.ContainsKey(cam.GetType()))
            {
                return;
            }

            cameraMap.Add(cam.GetType(), cam);
        }

        private void UnparentCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                if (cam != null)
                {
                    cam.transform.parent = transform.root;
                }
            }
        }

        private void ActivateCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                if (cam != null)
                {
                    cam.gameObject.SetActive(true);
                }
            }

            currentCamera = cameraMap.Values.FirstOrDefault(cam => cam is ThirdPersonCamera)
                ?? cameraMap.Values.FirstOrDefault();
        }

        private void DeactivateCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                if (cam != null)
                {
                    cam.gameObject.SetActive(false);
                }
            }
        }

        private void BringCameraToFront<T>() where T : PlayerCameraBase
        {
            foreach (var cam in cameraMap.Values)
            {
                cam?.SetPriority(0);
            }

            var target = GetCamera<T>();
            if (target != null)
            {
                target.SetPriority(1);
                currentCamera = target;
            }
        }
    }
}
