using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SimpleMP.Game
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawner settings")]
        public GameObject playerPrefab;
        public Transform[] spawnPoints;

        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            cam.gameObject.SetActive(true);
            if (IsServer)
            {
                StartCoroutine(DelayedAction());
            }
        }

        #region Timers
        private IEnumerator DelayedAction()
        {
            yield return new WaitForSeconds(4f);
            SpawnPlayers();
        }

        public IEnumerator DelayedRespawn(ulong clientId)
        {
            yield return new WaitForSeconds(10f);
            RespawnPlayer(clientId);
        }
        #endregion

        #region Spawners
        public void SpawnPlayers()
        {
            if (IsServer)
            {
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    GameObject player = Instantiate(playerPrefab, spawn.position, Quaternion.identity);
                    player.name = $"Player | Id: {clientId}";
                    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }

                TurnOffMainCameraClientRpc();
            }
            
        }

        public void RespawnPlayer(ulong clientId)
        {
            if (IsServer)
            {
                Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject player = Instantiate(playerPrefab, spawn.position, Quaternion.identity);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                player.name = $"Player | Id: {clientId}";

                var target = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { clientId }
                    }
                };

                TurnOffMainCameraClientRpc(target);
            }
        }

        #endregion

        #region Client RPCs
        [ClientRpc]
        private void TurnOffMainCameraClientRpc()
        {
            cam.gameObject.SetActive(false);
        }

        [ClientRpc]
        private void TurnOffMainCameraClientRpc(ClientRpcParams rpcParams = default)
        {
            cam.gameObject.SetActive(false);
        }

        #endregion
    }
}