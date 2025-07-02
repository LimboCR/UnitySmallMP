using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections.Generic;

namespace SimpleMP.Characters
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class ThirdPersonNetworkController : NetworkBehaviour
    {
        #region Variables
        #region Prefabs and references
        [Header("Necessary Prefabs")]
        [SerializeField] private GameObject playerHud;
        [SerializeField] private GameObject playerCameraPrefab;

        [Header("References")]
        [SerializeField] private Transform playerHead;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject playerHudRef;
        [SerializeField] private GameObject deathUI;

        [Header("Death Settings")]
        [SerializeField] private Material colorOnDeath;
        #endregion

        #region Mouse and Movement
        [Header("Mouse Settings")]
        [SerializeField] [Range(0.0f, 0.5f)] private float mouseSmoothTime = 0.03f;
        [SerializeField] private float mouseSensitivity = 3.5f;
        [SerializeField] private bool cursorLock = true;

        [Header("Movement")]
        [SerializeField] private float Speed = 9.0f;
        [SerializeField] private float RunSpeed = 18.0f;
        [SerializeField] [Range(0.0f, 0.5f)] private float moveSmoothTime = 0.3f;
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float jumpHeight = 6f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask ground;
        [SerializeField] private bool isGrounded;
        #endregion

        #region Combat Vars
        [Space, Header("Combat Variables")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform wandRoot;
        [SerializeField] private LayerMask aimLayerMask;
        [SerializeField] private float aimDistance = 200f;
        [SerializeField] private Transform wandTip;   // where shooting comes from

        [SerializeField] private bool isAiming;
        [SerializeField] private Vector3 currentAimPoint;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        #endregion

        #region Camera Setup
        [Space, Header("Camera Setup")]
        [Header("Camera Limits")]
        [SerializeField] private float maxDegreeUp = -90.0f;
        [SerializeField] private float maxDegreeDown = 90.0f;

        #region CinemachineCamera Setup
        [Header("Original Cinemachine Cam")]
        [SerializeField] private Vector3 originalShoulderOffset;
        [SerializeField] private Vector3 originalDamping;
        [SerializeField] private float originalVerticalCamLength;
        [SerializeField, Range(0f, 1f)] private float camSide = 1f;
        [SerializeField] private float camDistance = 3f;

        [Header("Aimed Cinemachine Cam")]
        [SerializeField] private Vector3 aimedShoulderOffset;
        [SerializeField] private Vector3 aimedDamping;
        [SerializeField] private float aimedVerticalCamLength;
        [SerializeField] private float aimedcamDistance = 3f;
        #endregion

        #endregion

        #region Interpolation vars
        [Space, Header("Interpolation Vars")]
        [Header("Interpolation (For Remote Clients)")]
        [SerializeField] private float headRotationLerpSpeed = 10f;

        #endregion

        #region TEST VARIABLES
        [Space, Header("===TEST MODES===")]
        [SerializeField] private bool TestAiming;
        [SerializeField] private bool LockMovement;
        [SerializeField] private bool LockCameraMovement;
        [SerializeField] private bool IgnoreApplicationFocused;
        [SerializeField] private bool TestDeath;
        [SerializeField] private bool TestKill;
        [SerializeField] private bool DebugProjectileHit;
        #endregion

        #region Private Variables (hidden)
        private float velocityY;
        

        private float cameraCap;
        private Vector2 currentMouseDelta;
        private Vector2 currentMouseDeltaVelocity;
        
        private Vector2 currentDir;
        private Vector2 currentDirVelocity;

        private Vector3 velocity;

        private bool _allSet = false;

        private Camera cam;
        private GameObject scoreCanvas;
        bool CheckPlayerSetup => controller != null && playerCameraPrefab != null && playerHead != null && wandTip != null && playerHealth != null
            && playerHud != null && playerCinemachine != null && groundCheck != null;

        private CinemachineCamera playerCinemachine;

        private MeshRenderer playerRenderer;
        #endregion

        #region NetworkVariables
        public NetworkVariable<Quaternion> HeadRotation = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> BulletsAmount = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Owner);
        public NetworkVariable<Vector3> NetworkAimPoint = new(writePerm: NetworkVariableWritePermission.Owner);
        #endregion

        #endregion

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (controller == null)
                Debug.LogError($"[ThirdPersonNetworkController] No CharacterController on {gameObject.name} (ClientId: {OwnerClientId})");

            cam = Camera.main;

            scoreCanvas = Mechanics.GameManager.Instance.GetScoreTabRef();

            playerRenderer = gameObject.GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            if (IsServer)
            {
                BulletsAmount.Value = 100;

                //if (!NetworkManager.Singleton.IsServer) return;
                //Debug.Log("[ThirdPersonNetworkController] [OnNetworkSpawn] called server action");
                Mechanics.ServerEventsManager.RegisterPlayerSpawned(OwnerClientId);
            }

            if (cursorLock && IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                BulletsAmount.OnValueChanged += OnBulletsAmountChanged;

                if (playerCameraPrefab != null)
                {
                    GameObject objRef = Instantiate(playerCameraPrefab, transform);
                    objRef.transform.localPosition = new(0f, 1.1f, 0f);
                    playerHead = objRef.transform;

                    playerCinemachine = objRef.GetComponentInChildren<CinemachineCamera>();
                    playerCamera = objRef.GetComponentInChildren<Camera>();
                    //if (playerCinemachine != null) Debug.Log("PlayerCamer was found");
                }
                else Debug.LogError("No playerCameraPrefab was found");

                //Player Hud Local Setup
                if (playerHud != null)
                {
                    playerHudRef = Instantiate(playerHud, transform);
                    //Debug.Log($"[ThirdPersonPlayer] [IsOwner] Player nickname from this player: {PlayerDataManager.Instance.GetNickname(OwnerClientId)}");

                    var hudManager = playerHudRef.GetComponent<PlayerHudManager>();
                    hudManager.SetPlayerNickname(PlayerDataManager.Instance.GetNickname(0));
                }
                else Debug.LogError("No PlayerHud prefab was found");

                //Player Health local setup
                playerHealth = GetComponent<Health>();
                if (playerHealth == null)
                    Debug.LogError($"[ThirdPersonNetworkController] No Health.cs on {gameObject.name} (ClientId: {OwnerClientId})");

                _allSet = CheckPlayerSetup;
            }
        }

        private void Update()
        {

            if (TestKill && IsServer)
            {
                TestKill = false;
                TestKillServerRpc();
            }

            if (TestDeath && IsServer)
            {
                TestDeath = false;
                TestDeathServerRpc();
            }

            if (_allSet)
            {
                if (playerHealth.Alive)
                {
                    if (IsOwner)
                    {
                        deathUI.SetActive(false);

                        HeadRotation.Value = playerHead.rotation; // Sync to server
                        if (!LockCameraMovement) UpdateMouse();
                        if (!LockMovement) UpdateMove();
                        HandleAiming();

                        if(BulletsAmount.Value > 0)
                        {
                            if (isAiming && Input.GetMouseButtonDown(0)) // Left click
                                ShootServerRpc(currentAimPoint);

                            else if (Input.GetMouseButtonDown(0) && !isAiming)
                            {
                                //ToDo: Implement shooting without aiming
                            }
                        }

                        if (Input.GetKeyDown(KeyCode.Tab))
                            scoreCanvas.SetActive(true);

                        if (Input.GetKeyUp(KeyCode.Tab))
                            scoreCanvas.SetActive(false);
                    }
                    else // Interpolate head movement for other clients
                        playerHead.rotation = Quaternion.Slerp(playerHead.rotation, HeadRotation.Value, Time.deltaTime * headRotationLerpSpeed);
                }
                else 
                {
                    if (IsOwner)
                    {
                        deathUI.SetActive(true);
                        playerHead.gameObject.SetActive(false);
                        cam.gameObject.SetActive(true);

                        if(colorOnDeath != null && playerRenderer.material != colorOnDeath)
                            playerRenderer.material = colorOnDeath;
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            if (IsOwner) //Unsubscribing from events for safety
            {
                BulletsAmount.OnValueChanged -= OnBulletsAmountChanged;
            }
            
        }

        #region Mouse Updaters
        private void UpdateMouse() //Updates mouse movement to move camera
        {
            Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, mouseSmoothTime);

            UpdateMouseServerRpc(currentMouseDelta.x); // Yaw to server
            UpdateCameraPitch(currentMouseDelta.y);    // Local-only pitch
        }

        private void UpdateCameraPitch(float mouseY) //Updates camera movement up & down
        {
            cameraCap -= mouseY * mouseSensitivity;
            cameraCap = Mathf.Clamp(cameraCap, maxDegreeUp, maxDegreeDown);
            playerHead.localEulerAngles = Vector3.right * cameraCap;
        }

        #endregion

        #region Movement Updater
        private void UpdateMove() //Responsible for movement in different directions
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, ground);

            Vector2 targetDir = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            targetDir.Normalize();

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                UpdateJumpVelocityServerRpc(Mathf.Sqrt(jumpHeight * -2f * gravity));
                isGrounded = false;
            }

            if (!isGrounded && controller.velocity.y < -1f)
                UpdateJumpVelocityServerRpc(-8f);

            if (Input.GetKey(KeyCode.LeftShift)) //Running logic ("Sprint" button does not work for some reason)
                UpdateMoveServerRpc(targetDir, RunSpeed);
            else
                UpdateMoveServerRpc(targetDir, Speed);

        }

        #endregion

        #region AimHandler
        private void HandleAiming()
        {
            if (IsOwner)
            {
                if (!TestAiming)
                    isAiming = Input.GetMouseButton(1); // Hold RMB to aim

                CinemachineThirdPersonFollow followRef = null;

                if (playerCinemachine != null)
                    followRef = playerCinemachine.GetComponent<CinemachineThirdPersonFollow>();

                followRef.CameraSide = camSide;

                if (!isAiming)
                {
                    if (followRef != null)
                    {
                        followRef.ShoulderOffset = originalShoulderOffset;
                        followRef.Damping = originalDamping;
                        followRef.VerticalArmLength = originalVerticalCamLength;
                        followRef.CameraDistance = camDistance;
                    }
                    return;
                }

                if (followRef != null)
                {
                    followRef.ShoulderOffset = aimedShoulderOffset;
                    followRef.Damping = aimedDamping;
                    followRef.VerticalArmLength = aimedVerticalCamLength;
                    followRef.CameraDistance = aimedcamDistance;
                }

                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayerMask))
                    currentAimPoint = hit.point;
                else
                    currentAimPoint = ray.GetPoint(100f);

                wandRoot.LookAt(currentAimPoint);              // Aim locally
                UpdateWandServerRpc(currentAimPoint);          // Send to server
            }
        }

        #endregion

        #region Helper
        private void OnBulletsAmountChanged(int oldValue, int newValue) //Track Bullets Amount On UI
        {
            Mechanics.ClientEventsManager.NotifyBulletsChanged(newValue);
        }
        #endregion

        #region Server RPCs
        [ServerRpc] //Server Movement Updater
        private void UpdateMoveServerRpc(Vector2 targetDir, float moveSpeed)
        {
            currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, moveSmoothTime);

            velocityY += gravity * 2f * Time.deltaTime;
            Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * moveSpeed + Vector3.up * velocityY;
            controller.Move(velocity * Time.deltaTime);
        }

        [ServerRpc] //Server Jump Updater
        private void UpdateJumpVelocityServerRpc(float value)
        {
            velocityY = value;
        }

        [ServerRpc] //Server Mouse Updater
        private void UpdateMouseServerRpc(float mouseX)
        {
            transform.Rotate(mouseSensitivity * mouseX * Vector3.up);
        }

        [ServerRpc] //Server Gun Aiming Updater
        private void UpdateWandServerRpc(Vector3 aimPoint)
        {
            wandRoot.LookAt(aimPoint);                      // Client-Server visual
            BroadcastWandLookAtClientRpc(aimPoint);         // Sync with all other clients
        }

        [ServerRpc] //Server Shooting Logic
        private void ShootServerRpc(Vector3 targetPoint)
        {
            BulletsAmount.Value--;

            GameObject proj = Instantiate(projectilePrefab, wandTip.position, wandTip.rotation);
            if (proj.TryGetComponent(out Weapons.Projectile bullet))
            {
                bullet.SetOwner(OwnerClientId); // works even for server AI (we use 999 clientId)
                if (DebugProjectileHit) bullet.DebugHit();
            }
                
            proj.GetComponent<NetworkObject>().Spawn();
            proj.GetComponent<Rigidbody>().linearVelocity = wandTip.forward * 220f;
        }

        #region Tests
        [ServerRpc] //Server Test Player Death Event
        private void TestDeathServerRpc()
        {
            playerHealth.TakeDamage(10000f, 999);
        }

        [ServerRpc] //Server Test Player Killed Event
        private void TestKillServerRpc()
        {
            Mechanics.ServerEventsManager.RegisterPlayerKilled(OwnerClientId);
        }
        #endregion

        #endregion

        #region Client RPCs
        [ClientRpc] //Show Other Clients Gun Movement
        private void BroadcastWandLookAtClientRpc(Vector3 aimPoint)
        {
            if (!IsOwner)
                wandRoot.LookAt(aimPoint);// Remote visuals
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsOwner || !isAiming) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(wandTip.position, currentAimPoint);
        }

        #endregion
    }
}
