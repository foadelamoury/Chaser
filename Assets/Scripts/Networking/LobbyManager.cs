using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class LobbyManager : MonoBehaviour
{
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "PlayerCharacter";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public static LobbyManager Instance;

    public event Action<Lobby> JoinedLobby;
    public event Action<Lobby> JoinedLobbyUpdate;
    public event Action<Lobby> KickedFromLobby;
    public event Action<Lobby> LobbyGameModeChanged;
    public event Action<List<Lobby>> LobbyListChanged;
    public event Action OnLeftLobby;

    public enum GameMode
    {
        HunterVsHunted,
        Racing
    }

    public enum PlayerCharacter
    {
        Hunter,
        Hunted
    }

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private float listLobbiesTimer;

    private string playerName;
    private bool isLocalPlayerJoining;
    private bool isGameStarting; // Prevent multiple StartGame calls

    [SerializeField] private string lobbyName = "Predator";
    [SerializeField] private int maxPlayers = 4;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Authenticate(string playerName)
    {
        this.playerName = playerName;
        AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            // Debug.Log("Signed in! Player ID: " + AuthenticationService.Instance.PlayerId);
            RefreshLobbyList();
        };


        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            // Debug.Log($"[NetworkManager] Client connected. Id: {clientId}");
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            // Debug.Log($"[NetworkManager] Client disconnected. Id: {clientId}");
        };

    }

    void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    async void HandleLobbyHeartbeat()
    {


        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                    // Debug.Log("Heartbeat sent");
                }
                catch (LobbyServiceException e)
                {
                    // Debug.Log(e.Message);
                    if (e.ErrorCode == 404 || e.Reason == LobbyExceptionReason.LobbyNotFound)
                    {
                        hostLobby = null;
                        joinedLobby = null;
                        OnLeftLobby?.Invoke();
                    }
                }
            }
        }
    }
    async void HandleLobbyPollForUpdates()
    {
        if (IsGameStarted() || isLocalPlayerJoining) return;

        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float lobbyUpdateTimerMax = 2.0f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                try
                {
                    Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                    joinedLobby = lobby;

                    JoinedLobbyUpdate?.Invoke(joinedLobby);


                    if (joinedLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
                    {
                        if (!IsLobbyHost())
                        {
                            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                            JoinRelay(relayJoinCode);
                        }
                        return;
                    }

                    if (!IsPlayerInLobby())
                    {
                        // Debug.Log("Kicked from Lobby!");
                        KickedFromLobby?.Invoke(joinedLobby);
                        joinedLobby = null;
                        return;
                    }

                    PrintPlayers(joinedLobby);
                }
                catch (LobbyServiceException e)
                {
                    // Debug.Log(e);
                    if (e.ErrorCode == 404 || e.Reason == LobbyExceptionReason.LobbyNotFound)
                    {
                        // Debug.Log("Lobby was closed or deleted.");
                        joinedLobby = null;
                        hostLobby = null;
                        OnLeftLobby?.Invoke();
                    }
                }
            }
        }
    }

    private async void JoinRelay(string joinCode)
    {
        if (isLocalPlayerJoining) return;
        isLocalPlayerJoining = true;

        try
        {
            // Debug.Log("Joining Relay with code: " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                joinAllocation.Key,
                false
            );


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartClient();

            if (!started)
            {
                // Debug.LogError("Failed to start client");
                isLocalPlayerJoining = false;
            }
        }
        catch (RelayServiceException e)
        {
            // Debug.LogError("RelayServiceException: " + e);
            isLocalPlayerJoining = false;
        }
        catch (System.Exception e)
        {
            // Debug.LogError("Exception in JoinRelay: " + e);
            isLocalPlayerJoining = false;
        }
    }

    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Unity.Services.Lobbies.Models.Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode)
    {
        try
        {
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "GameMode", new DataObject (DataObject.VisibilityOptions.Public , gameMode.ToString())
                    },
                    {
                        "Map", new DataObject (DataObject.VisibilityOptions.Public , "City")
                    }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            // Debug.Log("Created Lobby: " + lobby.Name + " with Max Players: " + lobby.MaxPlayers + " Lobby ID: " + lobby.Id + " Lobby Code: " + lobby.LobbyCode);
            JoinedLobby?.Invoke(lobby);

            PrintPlayers(hostLobby);

        }
        catch (LobbyServiceException e)
        {
            // Debug.LogError("Failed to create lobby: " + e);
        }

    }

    public void RefreshLobbyList()
    {
        ListLobbies();
    }


    async void ListLobbies()
    {
        try
        {
            if (Time.time - listLobbiesTimer < 3.0f)
            { // Increased to avoid rate limiting
                return;
            }
            listLobbiesTimer = Time.time;

            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false , QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            // Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            LobbyListChanged?.Invoke(queryResponse.Results);

            //queryResponse.Results.ForEach(
            //   lobby =>  Debug.Log("lobby name: " + lobby.Name + " lobby max players: " + lobby.MaxPlayers + " Game mode " + lobby.Data["GameMode"].Value));
        }
        catch (LobbyServiceException e)
        { // Debug.LogError("Failed to list lobbies: " + e);
        }

    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions { Player = GetPlayer() };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            joinedLobby = lobby;

            // Debug.Log("Joined Lobby: " + joinedLobby.Name + " with Max Players: " + joinedLobby.MaxPlayers + " with code " + lobbyCode);

            JoinedLobby?.Invoke(lobby);

            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        { // Debug.LogError("Failed to join lobby: " + e);
        }
    }


    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);

            this.joinedLobby = joinedLobby;

            // Debug.Log("Joined Lobby: " + joinedLobby.Name + " with Max Players: " + joinedLobby.MaxPlayers);

            JoinedLobby?.Invoke(joinedLobby);
            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            // Debug.Log(e);
        }
    }

    async void QuickJoinLobby()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            joinedLobby = lobby;
            JoinedLobby?.Invoke(lobby);
            // Debug.Log("Quick Joined Lobby: " + lobby.Name + " with Max Players: " + lobby.MaxPlayers);
        }
        catch (LobbyServiceException e)
        { // Debug.LogError("Failed to quick join lobby: " + e);
        }
    }

    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }

    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    void PrintPlayers(Lobby lobby)
    {
        // Debug.Log("Players in lobby " + lobby.Name + " " + lobby.Data[KEY_GAME_MODE].Value + " " + lobby.Data["Map"].Value);
        foreach (Unity.Services.Lobbies.Models.Player player in lobby.Players)
        {
            // Debug.Log("Player ID: " + player.Id + " Player Name: " + player.Data[KEY_PLAYER_NAME].Value);
        }
    }

    Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {
                    KEY_PLAYER_NAME, new PlayerDataObject (PlayerDataObject.VisibilityOptions.Member, playerName)
                },
                {
                    KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerCharacter.Hunter.ToString())
                }
            }
        };
    }

    public void ChangeGameMode()
    {
        if (IsLobbyHost())
        {
            GameMode gameMode = System.Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (gameMode)
            {
                default:
                case GameMode.Racing: gameMode = GameMode.HunterVsHunted; break;
                case GameMode.HunterVsHunted: gameMode = GameMode.Racing; break;
            }

            UpdateLobbyGameMode(gameMode.ToString());
        }
    }

    async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        KEY_GAME_MODE, new DataObject (DataObject.VisibilityOptions.Public , gameMode/*, DataObject.IndexOptions.S1*/)
                    }
                }
            });
            joinedLobby = hostLobby;

            LobbyGameModeChanged?.Invoke(joinedLobby);

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        { // Debug.LogError("Failed to update game mode: " + e);
        }

    }

    public async void UpdatePlayerName(string newPlayerName)
    {
        this.playerName = newPlayerName;

        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {
                            KEY_PLAYER_NAME, new PlayerDataObject (PlayerDataObject.VisibilityOptions.Member, playerName)
                        }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                // Debug.LogError("Failed to update player name: " + e);
            }
        }
    }

    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter)
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject> {
                        { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerCharacter.ToString()) }
                    }
                });
            }
            catch (LobbyServiceException e)
            { // Debug.Log(e); }
            }
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            if (joinedLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                OnLeftLobby?.Invoke();
            }
        }
        catch (LobbyServiceException e)
        {
            // Debug.LogError("Failed to leave lobby: " + e);
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                // Debug.LogError("Failed to kick player: " + e);
            }
        }
    }
    async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id
            });
            joinedLobby = hostLobby;
        }
        catch (LobbyServiceException e)
        {
            // Debug.LogError("Failed to migrate host: " + e);
        }
    }
    async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            // Debug.LogError("Failed to delete lobby: " + e);
        }
    }

    public async void StartGame()
    {
        if (isGameStarting)
        {
            // // Debug.LogWarning("StartGame already in progress, ignoring.");
            return;
        }

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            // // Debug.LogWarning("NetworkManager is already running, ignoring StartGame.");
            return;
        }

        if (IsLobbyHost())
        {
            isGameStarting = true;
            try
            {
                // Debug.Log("StartGame");
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                // Debug.Log($"Relay Allocation created. Server: {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}, Join Code: {relayJoinCode}");

                await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> {
                        { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                    }
                });

                RelayServerData relayServerData = new RelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.ConnectionData,
                    allocation.ConnectionData,
                    allocation.Key,
                    false // Use UDP instead of DTLS for reliability
                );

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.GetComponent<UnityTransport>() != null)
                {
                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetRelayServerData(relayServerData);

                    // Debug.Log($"Relay data set. Starting Host...");
                    bool hostStarted = NetworkManager.Singleton.StartHost();
                    // Debug.Log($"StartHost returned: {hostStarted}");

                    if (hostStarted)
                    {
                        // Wait for clients to connect before loading scene
                        int expectedClients = joinedLobby.Players.Count - 1; // Exclude host
                        // Debug.Log($"Waiting for {expectedClients} client(s) to connect...");
                        StartCoroutine(WaitForClientsAndLoadScene(expectedClients));
                    }
                    else
                    {
                        // Debug.LogError("Failed to start host!");
                        isGameStarting = false;
                    }
                }
                else
                {
                    // Debug.LogError("NetworkManager or UnityTransport missing! Please add a NetworkManager with UnityTransport to the scene.");
                    isGameStarting = false;
                }
            }
            catch (LobbyServiceException e)
            {
                // Debug.LogError("LobbyServiceException in StartGame: " + e);
                isGameStarting = false;
            }
            catch (System.Exception e)
            {
                // Debug.LogError("Exception in StartGame: " + e);
                isGameStarting = false;
            }
        }
    }

    private System.Collections.IEnumerator WaitForClientsAndLoadScene(int expectedClients)
    {
        float timeout = 10f; // Maximum wait time in seconds
        float elapsed = 0f;

        while (NetworkManager.Singleton.ConnectedClientsIds.Count < expectedClients + 1 && elapsed < timeout)
        { // +1 for host
            // Debug.Log($"Connected clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}, Expected: {expectedClients + 1}");
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= expectedClients + 1)
        {
            // Debug.Log("All clients connected! Loading Game scene...");
        }
        else
        {
            // // Debug.LogWarning($"Timeout reached. Only {NetworkManager.Singleton.ConnectedClientsIds.Count - 1} of {expectedClients} clients connected. Loading anyway...");
        }

        NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }


    private bool IsGameStarted()
    {
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;
        }
        return false;
    }
}
