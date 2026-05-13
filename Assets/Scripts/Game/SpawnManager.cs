using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Services.Authentication;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject hunterPrefab;
    [SerializeField] private GameObject huntedPrefab;
    [SerializeField] private GameObject pedestrianPrefab;
    public Transform[] spawnedClones;

    [SerializeField] private GameObject gameManager;
    public Transform[] spawnPoints;

    private static int spawnedHunterCount = 0;
    private static int spawnedHuntedCount = 0;

    private void Awake()
    {
        spawnedClones = new Transform[2];
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {
            spawnedHunterCount = 0;
            spawnedHuntedCount = 0;
        }
        SpawnGOsServerRpc(AuthenticationService.Instance.PlayerId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnGOsServerRpc(string playerId, RpcParams rpcParams = default)
    {
        // the default character is hunter
        LobbyManager.PlayerCharacter playerCharacter = LobbyManager.PlayerCharacter.Hunter;

        if (LobbyManager.Instance == null) return;
        if (LobbyManager.Instance.GetJoinedLobby() == null) return;

        foreach (Unity.Services.Lobbies.Models.Player player in LobbyManager.Instance.GetJoinedLobby().Players)
        {
            if (player.Id == playerId)
            {
                if (player.Data.ContainsKey(LobbyManager.KEY_PLAYER_CHARACTER))
                {
                    System.Enum.TryParse(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value, out playerCharacter);
                }
                break;
            }
        }

        if (playerCharacter == LobbyManager.PlayerCharacter.Hunter)
        {
            if (spawnedHunterCount >= 1)
            {
                playerCharacter = LobbyManager.PlayerCharacter.Hunted;
            }
        }
        
        if (playerCharacter == LobbyManager.PlayerCharacter.Hunter) 
        { 
            spawnedHunterCount++; 
        }
        else 
        { 
            spawnedHuntedCount++; 
        }

      

        GameObject characterClone;
        if (playerCharacter == LobbyManager.PlayerCharacter.Hunted)
        {
            characterClone = Instantiate(huntedPrefab, spawnPoints[0].position, Quaternion.identity);
            characterClone.gameObject.name = "Hunted";
            spawnedClones[0] = characterClone.transform;
        }
        else
        {
            characterClone = Instantiate(hunterPrefab, spawnPoints[1].position, Quaternion.identity);
            characterClone.gameObject.name = "Hunter";
            spawnedClones[1] = characterClone.transform;

        }
        characterClone.GetComponent<NetworkObject>().SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
        //Instantiate(pedestrianPrefab, spawnPoints[2].position, Quaternion.identity);
    }
}
