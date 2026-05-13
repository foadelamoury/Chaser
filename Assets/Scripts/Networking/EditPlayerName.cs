using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class EditPlayerName : MonoBehaviour {


    public static EditPlayerName Instance {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<EditPlayerName>();
            }
            return instance;
        }
        private set { instance = value; }
    }
    private static EditPlayerName instance;


    public event EventHandler OnNameChanged;


    [SerializeField] private TextMeshProUGUI playerNameText;


    private string playerName = "Hunter";


    private void Awake() {
        Instance = this;
       
        GetComponent<Button>().onClick.AddListener(ShowPlayerNameInputWindow);

        playerNameText.text = playerName;
    }

    public void ShowPlayerNameInputWindow() {
        UI_InputWindow.Show_Static("Player Name", playerName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
        () => {
            // Cancel
        },
        (string newName) => {
            playerName = newName;

            playerNameText.text = playerName;

            OnNameChanged?.Invoke(this, EventArgs.Empty);
        });
    }



    private void Start() {
        OnNameChanged += EditPlayerName_OnNameChanged;
    }

    private void EditPlayerName_OnNameChanged(object sender, EventArgs e) {
        LobbyManager.Instance.UpdatePlayerName(GetPlayerName());
    }

    public string GetPlayerName() {
        return playerName;
    }


}