using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour {


    public static LobbyListUI Instance { get; private set; }



    [SerializeField] private Transform lobbySingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createLobbyButton;


    private void Awake() {
        Instance = this;

        lobbySingleTemplate.gameObject.SetActive(false);

        refreshButton.onClick.AddListener(RefreshButtonClick);
        createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
    }

    private void Start() {
        if (LobbyManager.Instance == null) {
            Debug.LogError("LobbyManager.Instance is null! Make sure the LobbyManager object is in the scene and has the LobbyManager script attached.");
            return;
        }
        LobbyManager.Instance.LobbyListChanged += OnLobbyListChanged;
        LobbyManager.Instance.JoinedLobby += OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += OnLeftLobby;
        LobbyManager.Instance.KickedFromLobby += OnKickedFromLobby;
    }

    private void OnKickedFromLobby(Lobby lobby) {
        Show();
    }

    private void OnLeftLobby() {
        Show();
    }

    private void OnJoinedLobby(Lobby lobby) {
        Hide();
    }

    private void OnLobbyListChanged(List<Lobby> lobbyList) {
        UpdateLobbyList(lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList) {
        foreach (Transform child in container) {
            if (child == lobbySingleTemplate) continue;

            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList) {
            Transform lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            lobbySingleTransform.gameObject.SetActive(true);
            LobbyListSingleUI lobbyListSingleUI = lobbySingleTransform.GetComponent<LobbyListSingleUI>();
            lobbyListSingleUI.UpdateLobby(lobby);
        }
    }

    private void RefreshButtonClick() {
        LobbyManager.Instance.RefreshLobbyList();
    }

    private void CreateLobbyButtonClick() {
        LobbyCreateUI.Instance.Show();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
        gameObject.SetActive(true);
    }

}