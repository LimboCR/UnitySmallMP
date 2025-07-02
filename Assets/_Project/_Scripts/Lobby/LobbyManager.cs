using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using System.Text;

namespace SimpleMP
{
    public class LobbyManager : NetworkBehaviour
    {
        #region UI Variables
        [Header("UI Panels")]
        public GameObject mainMenuUI;
        public GameObject lobbyUI;

        [Space, Header("Players")]
        public GameObject playersList;
        public GameObject playerEntryPrefab;
        public TMP_InputField nicknameInput;

        [Space, Header("Buttons")]
        public Button hostButton;
        public Button clientButton;
        public Button leaveGame;
        public Button startGameButton;
        public Button leaveLobbyButton;

        [Space, Header("Status Controll")]
        public TMP_Text statusText;

        #endregion

        #region Dictionaries
        private Dictionary<ulong, string> playerNames = new();
        private Dictionary<ulong, GameObject> playerDisplayNames = new();

        public List<string> PlayerCheck = new();
        public List<string> DisplayPlayerCheck = new();

        #endregion

        private bool _hostInitiateShutdown = false;

        #region Start, Awake, Update and etc.
        void Start()
        {
            lobbyUI.SetActive(false);
            mainMenuUI.SetActive(true);

            hostButton.onClick.AddListener(OnHostClicked);
            clientButton.onClick.AddListener(OnClientClicked);
            startGameButton.onClick.AddListener(OnStartGameClicked);
            leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
            leaveGame.onClick.AddListener(OnLeaveGameClicked);

            startGameButton.gameObject.SetActive(false); // Hide until hosting
            statusText.text = "Choose Host or Client...";
        }

        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnDestroy()
        {
            //NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            //NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        #endregion

        #region Events Based Logic

        #region Buttons Logic
        void OnHostClicked()
        {
            string nickname = nicknameInput.text.Trim();

            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(nickname);

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

            NetworkManager.Singleton.StartHost();
            statusText.text = "Hosting...";

            mainMenuUI.SetActive(false);
            lobbyUI.SetActive(true);

            startGameButton.gameObject.SetActive(true);
        }

        void OnClientClicked()
        {            
            string nickname = nicknameInput.text.Trim();
            PlayerDataManager.Instance.SetPlayerNickname(OwnerClientId, nickname);
            //Debug.Log($"[OnClientClicked] Nickname of this client ({OwnerClientId}) is: {nickname} | PlayerDataManager : {PlayerDataManager.Instance.GetNickname(OwnerClientId)}");

            // Encode nickname and set as connection data
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(nickname);

            NetworkManager.Singleton.StartClient();
            statusText.text = "Connecting to host...";

            mainMenuUI.SetActive(false);
            lobbyUI.SetActive(true);

            startGameButton.gameObject.SetActive(false);
        }

        void OnLeaveGameClicked()
        {
            Application.Quit();
        }

        void OnStartGameClicked()
        {
            if (IsHost)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("GameWorld", LoadSceneMode.Single);
            }
        }

        void OnLeaveLobbyClicked()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                _hostInitiateShutdown = true;
                DisconnectAllClientRpc();
                NetworkManager.Singleton.ConnectionApprovalCallback = null;
            }

            if (!IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                LobbyCleanup();
                return;
            }

            // Disconnect or shutdown
            NetworkManager.Singleton.Shutdown();

            LobbyCleanup();

            _hostInitiateShutdown = false;
        }
        #endregion

        #region Netcode Events Logic
        void OnClientConnected(ulong clientId)
        {
            string nickname = PlayerDataManager.Instance.GetNickname(clientId);            

            // Build display name
            string displayName = clientId == NetworkManager.ServerClientId
                ? $"{nickname} (Host)"
                : $"{nickname}";

            // ✅ Host updates lobby list and notifies others
            if (IsHost)
            {
                // Save to authoritative lobby list
                playerNames[clientId] = displayName;
                PlayerCheck.Add(displayName); 

                GameObject go = Instantiate(playerEntryPrefab, playersList.transform);
                go.GetComponent<TMP_Text>().text = displayName;

                playerDisplayNames[clientId] = go;
                DisplayPlayerCheck.Add(displayName);

                var targetClient = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId }
                    }
                };
                
                SendLobbySnapshotClientRpc(GetLobbySnapshot(), targetClient);
                AppendPlayerClientRpc(clientId, displayName);
            }

            if (!IsHost)
            {
                statusText.text = "In lobby. Waiting for host to start...";
            }
        }

        void OnClientDisconnected(ulong clientId)
        {
            // Remove UI locally
            if (playerDisplayNames.TryGetValue(clientId, out var go))
            {
                if (!_hostInitiateShutdown)
                {
                    PlayerCheck.Remove(playerNames[clientId]);
                    DisplayPlayerCheck.Remove(playerNames[clientId]);
                }

                Destroy(go);
                playerDisplayNames.Remove(clientId);
                playerNames.Remove(clientId);
            }

            // Notify other clients to remove this player
            if (IsHost && !_hostInitiateShutdown)
            {
                var currentScene = SceneManager.GetActiveScene();
                if(currentScene.buildIndex == 0)
                    RemovePlayerClientRpc(clientId);
            }
        }

        #endregion

        #endregion

        #region Helpers

        void LobbyCleanup()
        {
            // Clear UI and player data
            playerNames.Clear();
            playerDisplayNames.Clear();

            PlayerCheck.Clear();
            DisplayPlayerCheck.Clear();

            foreach (Transform child in playersList.transform)
                Destroy(child.gameObject);

            // Return to main menu
            mainMenuUI.SetActive(true);
            lobbyUI.SetActive(false);
        }
        GameObject AddPlayerNameToLobby(string displayName)
        {
            GameObject go = Instantiate(playerEntryPrefab, playersList.transform);
            if (go.TryGetComponent(out TMP_Text text))
                text.text = displayName;
            return go;
        }

        private PlayerInfo[] GetLobbySnapshot()
        {
            var snapshot = new List<PlayerInfo>();
            foreach (var kvp in playerNames)
            {
                snapshot.Add(new PlayerInfo
                {
                    ClientId = kvp.Key,
                    PlayerName = kvp.Value
                });
            }
            return snapshot.ToArray();
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string nickname = Encoding.UTF8.GetString(request.Payload);
            ulong clientId = request.ClientNetworkId;

            PlayerDataManager.Instance.SetPlayerNickname(clientId, nickname);

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
        }

        #endregion

        #region RPCs
        [ClientRpc]
        private void SendLobbySnapshotClientRpc(PlayerInfo[] snapshot, ClientRpcParams clientRpcParams = default)
        {
            //Debug.Log($"[SendLobbySnapshotClientRpc] Called on OwnerClientId: {OwnerClientId}");

            foreach (var player in snapshot)
            {
                if (!playerDisplayNames.ContainsKey(player.ClientId))
                {
                    //Debug.Log($"[AppendPlayerClientRpc, client: {OwnerClientId}] !playerDisplayNames.ContainsKey(player.ClientId: {player.ClientId})");
                    var go = AddPlayerNameToLobby(player.PlayerName.ToString());
                    playerDisplayNames[player.ClientId] = go;
                    playerNames[player.ClientId] = player.PlayerName.ToString();

                    PlayerCheck.Add(player.PlayerName.ToString());
                    DisplayPlayerCheck.Add(player.PlayerName.ToString());

                }
            }
        }
        //Exexutes first on client, thats why it adds client in list first
        [ClientRpc]
        void AppendPlayerClientRpc(ulong clientId, string displayName)
        {
            //Debug.Log($"[AppendPlayerClientRpc] Called on OwnerClientId: {OwnerClientId}");

            if (!playerDisplayNames.ContainsKey(clientId))
            {
                //Debug.Log($"[AppendPlayerClientRpc, client: {OwnerClientId}] !playerDisplayNames.ContainsKey(clientId: {clientId})");
                var go = AddPlayerNameToLobby(displayName);
                playerDisplayNames[clientId] = go;
                playerNames[clientId] = displayName;

                PlayerCheck.Add(displayName);
                DisplayPlayerCheck.Add(displayName);
            }
        }

        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId)
        {
            //Debug.Log($"[RemovePlayerClientRpc, client: {OwnerClientId}] Called on OwnerClientId: {OwnerClientId}");

            if (playerDisplayNames.TryGetValue(clientId, out var go))
            {
                PlayerCheck.Remove(playerNames[clientId]);
                DisplayPlayerCheck.Remove(playerNames[clientId]);

                //playerDisplayNames[clientId] = null;
                //playerNames[clientId] = null;
                
                playerDisplayNames.Remove(clientId);
                playerNames.Remove(clientId);

                Destroy(go);
            }
        }

        [ClientRpc]
        private void DisconnectAllClientRpc()
        {
            if (!IsHost)
            {
                OnLeaveLobbyClicked();
            }
        }

        #endregion
    }

    public struct PlayerInfo : INetworkSerializable
    {
        public ulong ClientId;
        public FixedString32Bytes PlayerName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
        }
    }
}