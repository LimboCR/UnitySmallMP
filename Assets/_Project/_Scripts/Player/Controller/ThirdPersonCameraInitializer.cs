using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

namespace SimpleMP.Characters
{
    public class ThirdPersonCameraInitializer : NetworkBehaviour
    {
        [SerializeField] private Transform followTarget; // CameraFollowTarget from player prefab
        [SerializeField] private GameObject cam;

        private void Start()
        {
            if (!IsOwner) return;

            // Find the main Cinemachine camera
            CinemachineCamera virtualCam;
            cam.TryGetComponent(out virtualCam);
            if (virtualCam != null && followTarget != null)
            {
                //var thirdPerson = virtualCam.GetComponent<Cinemachine3rdPersonFollow>();
                //if (thirdPerson != null)
                //{
                //    thirdPerson.targ = followTarget;
                //}

                virtualCam.Priority = 100; // Ensure it's active
            }
        }
    }
}