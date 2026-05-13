using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {


    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button changeMarineButton;
    [SerializeField] private Button changeNinjaButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button startGameButton;


    private void Awake() {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        changeMarineButton.onClick.AddListener(() => {
            if (LobbyManager.Instance != null) {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Hunter);
            }
        });
        changeNinjaButton.onClick.AddListener(() => {
            if (LobbyManager.Instance != null) {
                LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Hunted);
            }
        });
     

        leaveLobbyButton.onClick.AddListener(() => {
            if (LobbyManager.Instance != null) {
                LobbyManager.Instance.LeaveLobby();
            }
        });

        changeGameModeButton.onClick.AddListener(() => {
            if (LobbyManager.Instance != null) {
                LobbyManager.Instance.ChangeGameMode();
            }
        });

        startGameButton.onClick.AddListener(() => {
            if (LobbyManager.Instance != null) {
                LobbyManager.Instance.StartGame();
            }
        });
    }

    private void Start() {
        if (LobbyManager.Instance == null) {
            Debug.LogError("LobbyManager.Instance is null! Make sure the LobbyManager object is in the scene and has the LobbyManager script attached.");
            return;
        }
        LobbyManager.Instance.JoinedLobby += UpdateLobby_Event;
        LobbyManager.Instance.JoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.LobbyGameModeChanged += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.KickedFromLobby += LobbyManager_OnKicked;

        Hide();
    }

    private void LobbyManager_OnLeftLobby() {
        ClearLobby();
        Hide();
    }

    private void LobbyManager_OnKicked(Lobby lobby) {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(Lobby lobby) {
        UpdateLobby();
    }

    private void UpdateLobby() {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby) {
        ClearLobby();

        foreach (Unity.Services.Lobbies.Models.Player player in lobby.Players) {
            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
            );

            lobbyPlayerSingleUI.UpdatePlayer(player);
        }

        changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
        startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;

        Show();
    }

    private void ClearLobby() {
        foreach (Transform child in container) {
            if (child == playerSingleTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void OnDestroy() {
        if (LobbyManager.Instance != null) {
            LobbyManager.Instance.JoinedLobby -= UpdateLobby_Event;
            LobbyManager.Instance.JoinedLobbyUpdate -= UpdateLobby_Event;
            LobbyManager.Instance.LobbyGameModeChanged -= UpdateLobby_Event;
            LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
            LobbyManager.Instance.KickedFromLobby -= LobbyManager_OnKicked;
        }
    }
}