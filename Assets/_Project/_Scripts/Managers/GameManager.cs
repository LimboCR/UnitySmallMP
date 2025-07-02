using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace SimpleMP.Mechanics
{
    public class GameManager : NetworkBehaviour
    {
        #region Variables

        #region Instance
        public static GameManager Instance { get; private set; }
        #endregion

        #region Score Tab and Spawners
        [Header("Visual PlayerInfo Data")]
        public List<PlayerInfo> PlayersInGame = new();
        
        [Header("Score Tab")]
        [SerializeField] private Transform scoreTabRoot;
        [SerializeField] private GameObject playerScoreTabPrefab;
        [SerializeField] private GameObject scoreCanvas;

        [Header("Spawners")]
        [SerializeField] private Game.PlayerSpawner playerSpawner;
        #endregion

        #region Debug and test
        [Space, Header("Debug Variables")]
        [SerializeField] private bool DebugClientRpcs = false;
        [SerializeField] private bool DebugServerRegisters = false;
        [SerializeField] private bool DebugQueue = false;
        #endregion

        #region Necessary Dictionaries & Queues
        public Dictionary<ulong, PlayerInfo> PlayersDict = new();
        public Dictionary<ulong, PlayerScoreTab> scoreTabPairs = new();
        private readonly Queue<ulong> playersToRespawn = new();
        #endregion

        #endregion

        #region Awake, Start, Update, etc...
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            playerSpawner = GameObject.Find("PlayerSpawner").GetComponent<Game.PlayerSpawner>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            //DontDestroyOnLoad(this);

            ServerEventsManager.OnPlayerSpawned.AddListener(RegisterPlayerSpawn);
            ServerEventsManager.OnPlayerRespawned.AddListener(RegisterPlayerRespawn);
            ServerEventsManager.OnPlayerDied.AddListener(RegisterDeath);
            ServerEventsManager.OnPlayerKilled.AddListener(RegisterKill);
        }

        private void OnDisable()
        {
            ServerEventsManager.OnPlayerSpawned.RemoveListener(RegisterPlayerSpawn);
            ServerEventsManager.OnPlayerRespawned.RemoveListener(RegisterPlayerRespawn);
            ServerEventsManager.OnPlayerDied.RemoveListener(RegisterDeath);
            ServerEventsManager.OnPlayerKilled.RemoveListener(RegisterKill);
        }

        private void Update()
        {
            if (!IsServer) return;

            if (playersToRespawn.Count > 0)
            {
                ulong clientToRespawn = playersToRespawn.Dequeue();
                StartCoroutine(playerSpawner.DelayedRespawn(clientToRespawn));

                if (DebugQueue)
                {
                    Debug.Log($"[GameManager] [Update] [RespawnQueue] called for {clientToRespawn}");
                }
            }
        }

        #endregion

        #region Server Registers Logic
        public void RegisterDeath(ulong clientId)
        {
            if (!IsServer) return;
            if (DebugServerRegisters)
            {
                Debug.Log($"[GameManager] [RegisterDeath] called with clientId: {clientId}");
            }

            playersToRespawn.Enqueue(clientId);

            if (PlayersDict.ContainsKey(clientId))
            {
                PlayersDict[clientId].PlayerDeaths++;
                if (scoreTabPairs.ContainsKey(clientId))
                    scoreTabPairs[clientId].UpdateDeaths(PlayersDict[clientId].PlayerDeaths);

                RegisterPlayerDeathScoreOnClientRpc(clientId, PlayersDict[clientId].PlayerDeaths);
            }
        }

        public void RegisterKill(ulong clientId)
        {
            if (!IsServer) return;

            if (DebugServerRegisters)
            {
                Debug.Log($"[GameManager] [RegisterKill] called with clientId: {clientId}");
            }

            if (PlayersDict.TryGetValue(clientId, out PlayerInfo player))
            {
                player.PlayerKills++;
                if (scoreTabPairs.TryGetValue(clientId, out PlayerScoreTab playerTab))
                    playerTab.UpdateKills(player.PlayerKills);
                else Debug.LogWarning($"[RegisterKill] [ScoreTabKillUpdate] No player with {clientId} was found in score tab");

                RegisterPlayerKillScoreOnClientRpc(clientId, player.PlayerKills);
                UpdateKillScoreUiForClientRpc(player.PlayerKills, GetTargetFromId(clientId));
            }
            else Debug.LogWarning($"[RegisterKill] [PlayersDict] No player with {clientId} was found in players  dictionary");
        }

        public void RegisterPlayerRespawn(ulong clientId)
        {
            if (!IsServer) return;

            if (DebugServerRegisters)
            {
                Debug.Log($"[GameManager] [RegisterPlayerRespawn] called with clientId: {clientId}");
            }
        } //Unimplemented yet

        private void RegisterPlayerSpawn(ulong clientId)
        {
            if (!IsServer) return;
            if (DebugServerRegisters)
            {
                Debug.Log("[GameManager] [RegisterPlayerSpawn] called");
            }

            var nickname = PlayerDataManager.Instance.GetNickname(clientId);
            
            if (!PlayersDict.ContainsKey(clientId))
            {
                if (DebugServerRegisters)
                {
                    Debug.Log("[GameManager] [RegisterPlayerSpawn] Player is not in dictionary yet");
                }

                var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject?.gameObject;
                if (playerObj == null)
                {
                    Debug.LogWarning($"[GameManager] Couldn't find player GameObject for client {clientId}");
                }

                PlayerInfo info = new()
                {
                    PlayerId = clientId,
                    PlayerNickname = nickname,
                    PlayerKills = 0,
                    PlayerDeaths = 0,
                    PlayerObj = playerObj
                };

                PlayersInGame.Add(info);
                PlayersDict[clientId] = info;

                if (DebugServerRegisters)
                {
                    Debug.Log($"[GameManager] Registered player {nickname} with ID {clientId}");
                }

                if (scoreTabRoot != null)
                {
                    scoreTabPairs[clientId] = Instantiate(playerScoreTabPrefab, scoreTabRoot).GetComponent<PlayerScoreTab>();
                    scoreTabPairs[clientId].SetNickname(nickname);
                    scoreTabPairs[clientId].UpdateKills(0);
                    scoreTabPairs[clientId].UpdateDeaths(0);
                }

                RegisterPlayerSpawnOnClientRpc(clientId, nickname); //Registering player to other clients (for score tab now)
            }
            else UpdateKillScoreUiForClientRpc(PlayersDict[clientId].PlayerKills, GetTargetFromId(clientId)); // Updating kill score hud for specified client on first spawn / after respawn
        }

        #endregion

        #region Getters
        public int GetClientKills(ulong clientId) => PlayersDict.ContainsKey(clientId) ? PlayersDict[clientId].PlayerKills : -1;          // -1 if client is not in player dictionary
        public int GetClientDeaths(ulong clientId) => PlayersDict.ContainsKey(clientId) ? PlayersDict[clientId].PlayerDeaths : -1;        // -1 if client is not in player dictionary
        public string GetClientNick(ulong clientId) => PlayersDict.ContainsKey(clientId) ? PlayersDict[clientId].PlayerNickname : null;   // null if client is not in player dictionary
        public GameObject GetScoreTabRef() => scoreCanvas;                                                                                // getter for other clients to have reference (can't find inactive gameobjects by name)

        #endregion

        #region Helpers
        // Helps getting target by clientId to execute on specific client
        private ClientRpcParams GetTargetFromId(ulong clientId) 
        {
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                { TargetClientIds = new List<ulong> { clientId } }
            };

            return target;
        }
        #endregion

        #region ClientRpcs
        [ClientRpc]
        private void RegisterPlayerSpawnOnClientRpc(ulong clientId, string nickname)
        {
            if (DebugClientRpcs)
            {
                Debug.Log($"[GameManager] [RegisterPlayerSpawnOnClientRpc] Called on {OwnerClientId} with values | id:{clientId}, nick: {nickname} |");
            }

            if (!PlayersDict.ContainsKey(clientId))
            {
                PlayerInfo info = new()
                {
                    PlayerId = clientId,
                    PlayerNickname = nickname,
                    PlayerKills = 0,
                    PlayerDeaths = 0,
                    PlayerObj = null
                };

                PlayersDict[clientId] = info;
                if (!PlayersInGame.Contains(info))
                    PlayersInGame.Add(info);

                if (scoreTabRoot != null)
                {
                    scoreTabPairs[clientId] = Instantiate(playerScoreTabPrefab, scoreTabRoot).GetComponent<PlayerScoreTab>();
                    scoreTabPairs[clientId].SetNickname(nickname);
                    scoreTabPairs[clientId].UpdateKills(0);
                    scoreTabPairs[clientId].UpdateDeaths(0);
                }
            }
        }

        [ClientRpc]
        private void RegisterPlayerKillScoreOnClientRpc(ulong clientId, int value)
        {
            if (DebugClientRpcs)
            {
                Debug.Log($"[GameManager] [RegisterPlayerKillScoreOnClientRpc] Called on {OwnerClientId} with values | id:{clientId}, value: {value} |");
            }

            if (PlayersDict.TryGetValue(clientId, out PlayerInfo player))
            {
                player.PlayerKills = value;

                if (scoreTabPairs.TryGetValue(clientId, out PlayerScoreTab playerTab))
                    playerTab.UpdateKills(player.PlayerKills);
                else Debug.LogWarning($"[RegisterPlayerKillScoreOnClientRpc] [ScoreTabKillUpdate] No player with {clientId} was found in score tab");
            }
        }

        [ClientRpc]
        private void RegisterPlayerDeathScoreOnClientRpc(ulong clientId, int value)
        {
            if (DebugClientRpcs)
            {
                Debug.Log($"[GameManager] [RegisterPlayerDeathScoreOnClientRpc] Called on {OwnerClientId} with values | id:{clientId}, value: {value} |");
            }

            if (PlayersDict.TryGetValue(clientId, out PlayerInfo player))
            {
                player.PlayerDeaths = value;

                if (scoreTabPairs.TryGetValue(clientId, out PlayerScoreTab playerTab))
                    playerTab.UpdateDeaths(player.PlayerDeaths);
                else Debug.LogWarning($"[RegisterPlayerKillScoreOnClientRpc] [ScoreTabDeathsUpdate] No player with {clientId} was found in score tab");
            }
        }

        [ClientRpc]
        private void UpdateKillScoreUiForClientRpc(int value, ClientRpcParams rpcParams = default)
        {
            if (DebugClientRpcs)
            {
                Debug.Log($"[GameManager] [UpdateKillScoreUiForClientRpc] Called on {OwnerClientId} with values | value:{value} |");
            }

            ClientEventsManager.NotifyKillScoreUI(value);
        }
        #endregion
    }

    [Serializable]
    public class PlayerInfo // Storage to track players data, keep consistency and avoid data change from clients side 
    {
        [Header("PlayerData")]
        public ulong PlayerId;
        public string PlayerNickname;

        [Header("Player Stats")]
        public int PlayerKills;
        public int PlayerDeaths;

        [Header("Player Reference")]
        public GameObject PlayerObj; //Unimplemented usage for now (Later will be used in matches with limited lives to check if alive)
    }
}

