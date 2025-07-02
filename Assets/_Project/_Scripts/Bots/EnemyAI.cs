using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace SimpleMP.AI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Characters.Health))]
    public class EnemyAI : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform gunJoint;
        [SerializeField] private Transform shootingPoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Characters.Health botHealth;

        private NavMeshAgent agent;
        private Transform targetPlayer;

        [Header("Combat Settings")]
        [SerializeField] private float sightRange = 25f;
        [SerializeField] private float attackRange = 12f;
        [SerializeField] private float shootCooldown = 1.5f;
        [SerializeField] private float forgetTargetDelay = 3f;
        [SerializeField] private LayerMask visibilityMask;
        [SerializeField] private float aimPrecision = 1f;
        [SerializeField] private Vector3 gunForwardAxisCorrection = new(0, 0, 0);

        [Space, Header("Verification variables (DO NOT EDIT)")]
        [SerializeField, Tooltip("Shows distance from bot to player")] private float distance;
        [SerializeField] private float lastShotTime;
        [SerializeField] private float lastSeenTime;
        [SerializeField] private bool canSeeTarget;

        [Space, Header("Debug Variables")]
        [SerializeField] private bool debugTargetRay;
        [SerializeField] private bool debugAimingRay;
        [SerializeField] private bool debugProjectileHit;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (TryGetComponent(out Characters.Health hp))
                botHealth = hp;
            else
                Debug.LogError($"No Health script found for {gameObject.name}, InstanceID: {gameObject.GetInstanceID()}");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                enabled = false;
        }

        private void Update()
        {
            if (botHealth.Alive)
            {
                targetPlayer = FindClosestPlayer();
                if (targetPlayer == null || targetPlayer.CompareTag("Dead")) return;

                distance = Vector3.Distance(transform.position, targetPlayer.position);
                Vector3 targetPos = targetPlayer.position;
                Vector3 direction = (targetPos - shootingPoint.position).normalized;

                // Check line of sight (from eyes or gun)
                canSeeTarget = Physics.Raycast(shootingPoint.position, direction, out RaycastHit hit, sightRange, visibilityMask)
                               && hit.collider.CompareTag("Player");

                if (canSeeTarget)
                    lastSeenTime = Time.time;

                bool recentlySeen = Time.time - lastSeenTime <= forgetTargetDelay;

                // Follow if not in range or lost sight
                if (distance > attackRange || !recentlySeen)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPlayer.position);
                }

                // Always rotate gun if in aim range
                if (distance < sightRange)
                    RotateGunToTarget(direction);

                // Try shooting when close + has LoS + gun facing player
                if (distance < attackRange && canSeeTarget && IsGunAimingCorrectly(direction))
                {
                    agent.isStopped = true; // stop only when really shooting

                    if (Time.time - lastShotTime > shootCooldown)
                    {
                        Shoot();
                        lastShotTime = Time.time;
                    }
                }

                Debug.DrawRay(shootingPoint.position, gunJoint.forward * attackRange, Color.red);
                Debug.DrawLine(shootingPoint.position, targetPlayer.position, Color.green);
            }

            else
            {
                agent.isStopped = true;
            }
        }

        private Transform FindClosestPlayer()
        {
            float minDist = float.MaxValue;
            Transform closest = null;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var obj = client.PlayerObject;
                if (obj == null) continue;

                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = obj.transform;
                }
            }

            return closest;
        }

        private void RotateGunToTarget(Vector3 dir) //Smooth gun rotation
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            gunJoint.rotation = Quaternion.Slerp(
                gunJoint.rotation,
                targetRot * Quaternion.Euler(gunForwardAxisCorrection),
                Time.deltaTime * 6f
            );
        }


        private bool IsGunAimingCorrectly(Vector3 dir)
        {
            float angle = Vector3.Angle(gunJoint.forward, dir);
            return angle <= aimPrecision * 10f;
        }

        private void Shoot()
        {
            GameObject proj = Instantiate(projectilePrefab, shootingPoint.position, gunJoint.rotation);
            var projScript = proj.GetComponent<Weapons.Projectile>();
            projScript.SetOwner(999);
            if (debugProjectileHit) projScript.DebugHit();
            proj.GetComponent<NetworkObject>().Spawn();
            proj.GetComponent<Rigidbody>().linearVelocity = shootingPoint.forward * 200f;
        }
    }
}
