using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticateUI : MonoBehaviour {


    [SerializeField] private Button authenticateButton;

   


    private void Awake() {
        authenticateButton.onClick.AddListener(() => {
            if (LobbyManager.Instance == null) {
                Debug.LogError("LobbyManager.Instance is null! Check if LobbyManager is in the scene.");
                return;
            }
            if (EditPlayerName.Instance == null) {
                Debug.LogError("EditPlayerName.Instance is null! Check if EditPlayerName is in the scene.");
                return;
            }
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
            Hide();
            EditPlayerName.Instance.ShowPlayerNameInputWindow();
        });

    }


    private void Hide() {
        gameObject.SetActive(false);
    }

}