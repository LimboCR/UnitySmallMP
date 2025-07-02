using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Limbo.ArrayUtils.ListExtensions;

namespace SimpleMP.Game
{
    public class BotSpawner : NetworkBehaviour
    {
        [Header("Spawner settings")]
        [SerializeField] private GameObject botPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int botsPerWave;
        [SerializeField] private int wavesAmount;
        [SerializeField] private List<GameObject> spawnedBotsReferences;

        private bool waveInAction = false;
        private bool spawningBots = false;
        private void Start()
        {
            if (IsServer)
            {
                spawningBots = true;
                StartCoroutine(DelayedSpawn());
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (!spawningBots)
            {
                if (waveInAction == false && wavesAmount > 0 && spawnedBotsReferences.Count <= 0)
                {
                    spawningBots = true;
                    StartCoroutine(DelayedSpawn());
                }

                if(spawnedBotsReferences.Count<=0) waveInAction = false;
            }

            if (spawnedBotsReferences.Count > 0) spawnedBotsReferences.SafeCleaner();
        }

        #region Timers
        private IEnumerator DelayedSpawn()
        {
            yield return new WaitForSeconds(10f);
            SpawnBots();
        }
        #endregion

        #region Spawners
        public void SpawnBots()
        {
            if (IsServer)
            {
                for(int i = 0; i<botsPerWave; i++)
                {
                    Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    GameObject bot = Instantiate(botPrefab, spawn.position, Quaternion.identity);
                    bot.name = $"BotWalker {i + 1}";
                    bot.GetComponent<NetworkObject>().Spawn(true);
                    spawnedBotsReferences.Add(bot);
                }

                wavesAmount--;
                waveInAction = true;
                spawningBots = false;
            }
        }
        #endregion
    }
}

