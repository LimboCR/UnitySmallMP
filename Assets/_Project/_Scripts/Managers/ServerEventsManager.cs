using System;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleMP.Mechanics
{
    public class ServerEventsManager : MonoBehaviour
    {
        public static ServerEventsManager Instance { get; private set; }

        public static UnityEvent<ulong> OnPlayerSpawned = new();
        public static UnityEvent<ulong> OnPlayerDied = new();
        public static UnityEvent<ulong> OnPlayerKilled = new();

        public static UnityEvent<ulong> OnPlayerDisconnected = new();
        public static UnityEvent<ulong> OnPlayerRespawned = new();

        private static bool DebugServerEvenets = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void RegisterPlayerSpawned(ulong clientId)
        {
            if (DebugServerEvenets)
            {
                Debug.Log($"[ServerEventsManager] [RegisterPlayerSpawned] called for {clientId}");
            }

            OnPlayerSpawned.Invoke(clientId);
        }

        public static void RegisterPlayerDied(ulong victimId)
        {
            if (DebugServerEvenets)
            {
                Debug.Log($"[ServerEventsManager] [RegisterPlayerDied] called for {victimId}");
            }

            OnPlayerDied.Invoke(victimId);
        }

        public static void RegisterPlayerKilled(ulong killerId)
        {
            if (DebugServerEvenets)
            {
                Debug.Log($"[ServerEventsManager] [RegisterPlayerKilled] called for {killerId}");
            }

            OnPlayerKilled.Invoke(killerId);
        }

        public static void RegisterPlayerDisconnected(ulong clientId) //Will destroy the traces and evidence of player being here
        {
            if (DebugServerEvenets)
            {
                Debug.Log($"[ServerEventsManager] [RegisterPlayerDisconnected] called for {clientId}");
            }

            OnPlayerDisconnected.Invoke(clientId);
        }

        public static void RegisterPlayerRespawned(ulong clientId) //Unused for now
        {
            if (DebugServerEvenets)
            {
                Debug.Log($"[ServerEventsManager] [RegisterPlayerRespawned] called for {clientId}");
            }

            OnPlayerRespawned.Invoke(clientId);
        }
    }
}

