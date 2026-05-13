using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button kickPlayerButton;


    private Unity.Services.Lobbies.Models.Player player;


    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible) {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Unity.Services.Lobbies.Models.Player player) {
        this.player = player;
        if (playerNameText != null) {
            if (player.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_NAME)) {
                playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
            } else {
                playerNameText.text = "Unknown Player";
            }
        } else {
            Debug.LogError("playerNameText is null in LobbyPlayerSingleUI!", transform);
        }

        if (characterImage != null) {
            if (player.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_CHARACTER)) {
                try {
                    LobbyManager.PlayerCharacter playerCharacter =
                        System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
                    characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);
                } catch (System.Exception e) {
                    Debug.LogError("Failed to parse PlayerCharacter: " + e);
                }
            }
        } else {
             Debug.LogError("characterImage is null in LobbyPlayerSingleUI!", transform);
        }
    }

    private void KickPlayer() {
        if (player != null) {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }


}