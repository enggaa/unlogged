using System;
using System.Collections;
using System.Collections.Generic;
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

            // playerCameras 리스트를 타입별로 cameraMap에 등록
            if (playerCameras != null)
            {
                foreach (var cam in playerCameras)
                {
                    if (cam != null && !cameraMap.ContainsKey(cam.GetType()))
                    {
                        cameraMap.Add(cam.GetType(), cam);
                    }
                }
            }

            DeactivateCameras();
        }

        private void Start()
        {
            // * Unparent so the player movement/rotation doesn't incorrectly affect cameras
            UnparentCameras();
            ActivateCameras();
        }

        public void RegisterCamera<T>(T camera) where T : PlayerCameraBase
        {
            cameraMap.Add(typeof(T), camera);
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

        private void UnparentCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                cam.transform.parent = transform.root;
            }
        }

        private void ActivateCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                cam.gameObject.SetActive(true);
            }
        }

        private void DeactivateCameras()
        {
            foreach (var cam in cameraMap.Values)
            {
                cam.gameObject.SetActive(false);
            }
        }

        private void BringCameraToFront<T>() where T : PlayerCameraBase
        {
            foreach (var cam in cameraMap.Values)
            {
                cam.SetPriority(0);
            }
            this.GetCamera<T>().SetPriority(1);
        }

        private void DoForAllCameras()
        {

        }
    }
}