using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SimpleMP.Weapons
{
    public class Projectile : NetworkBehaviour
    {
        private ulong _projectileOwner;
        [SerializeField] private bool debugHit = false;

        [Header("Debug Marker")]
        [SerializeField] private GameObject debugHitMarkerPrefab;

        private void Start()
        {
            if (IsServer)
                StartCoroutine(AutoDespawn());
        }

        public void SetOwner(ulong value)
        {
            _projectileOwner = value;
        }

        public void DebugHit()
        {
            debugHit = true;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!IsServer) return;

            GameObject obj = other.gameObject;
            //Debug.Log($"Projectile hit {obj.name} with tag: {obj.tag}");

            if (obj.CompareTag("Props"))
            {
                if(obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    var rbRef = this.GetComponent<Rigidbody>();
                    rb.linearVelocity = rbRef.linearVelocity * 0.1f;
                }
            }

            if(obj.CompareTag("Enemy") || obj.CompareTag("Player"))
            {
                if(obj.TryGetComponent(out Characters.IDamageble damageble))
                {
                    damageble.TakeDamage(10f, _projectileOwner);
                }
            }

            // 💥 Spawn debug marker
            if (debugHit && other.contacts.Length > 0)
            {
                Vector3 hitPoint = other.contacts[0].point;
                SpawnDebugMarker(hitPoint, obj.transform);
            }

            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Debug.LogWarning($"[Projectile] Tried to despawn but NetworkObject was not spawned or already despawned. Name: {gameObject.name}");
        }

        private void SpawnDebugMarker(Vector3 position, Transform parent)
        {
            if (debugHitMarkerPrefab != null)
            {
                GameObject marker = Instantiate(debugHitMarkerPrefab, position, Quaternion.identity, parent);
                Destroy(marker, 5f); // auto-destroy after 5 seconds
            }
            else Debug.LogWarning("No debugHitMarkerPrefab assigned on projectile.");
        }

        private IEnumerator AutoDespawn()
        {
            yield return new WaitForSeconds(3f);

            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn();
        }

    }

}