using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
// using static LobbyManager; // Be careful with statics in Network code

public class GameManager : NetworkSingleton<GameManager>
{
    [SerializeField] TextMeshProUGUI winningText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI ScoreText;

    

    private float[] _scores;

    private bool isRoundActive = true;
  
    [SerializeField] float localRoundTimer = 120f;
    public NetworkVariable<float> roundTimer = new NetworkVariable<float>(60f);
    [SerializeField] SpawnManager spawnManager;

    float constantRoundTimer;

    public override void Awake()
    {
        base.Awake();

        roundTimer.Value = localRoundTimer;
        _scores = new float[2];
        for (int i =0; i < _scores.Length; i++)
        {
            _scores[i] = 0;
        }
        constantRoundTimer = localRoundTimer;

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isRoundActive = true;
            roundTimer.Value = localRoundTimer;
        }
    }


    [ServerRpc]
    public void UpdateScoreServerRpc(int HunterOrHunted)
    {
     
        UpdateScoreClientRpc(HunterOrHunted);
    }

    [ClientRpc]

    public void UpdateScoreClientRpc(int HunterOrHunted)
    {
        if (HunterOrHunted == 0)
        {
            _scores[0]++;
        }
        else
        {
            _scores[1]++;
        }
        ScoreText.text = _scores[0] + " : " + _scores[1];

    }



    void Update()
    {
        if (!IsSpawned) return;
        
        if (IsServer)
        {
            if (localRoundTimer > 0 && isRoundActive)
            {
                localRoundTimer -= Time.deltaTime;
                roundTimer.Value = localRoundTimer;
            }
            else if (isRoundActive)
            {
                EndGame("The collector wins!");
                timerText.gameObject.SetActive(false);
                UpdateScoreServerRpc(1); // The thief wins
                ResetPosition(spawnManager.spawnedClones[0].transform, spawnManager.spawnedClones[1].transform);
                localRoundTimer = 0;
                roundTimer.Value = 0;
                isRoundActive = false;
            }
        }

            timerText.text = ((int)roundTimer.Value).ToString();
    }



    public void EndGame(string winner)
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.IsListening) WinningGameClientRpc(winner);

        }
    }

    [ClientRpc]
    private void WinningGameClientRpc(string winner)
    {
        if (winningText != null)
        {
             winningText.text = winner;
             winningText.gameObject.SetActive(true);
            roundTimer.Value = constantRoundTimer;
            Invoke(nameof(HideWinningText), 3f);

        }
    }

    void HideWinningText()
    {
        winningText.gameObject.SetActive(false);
    }   

    public void ResetPosition(Transform Hunter, Transform Hunted)
    {
        Hunter.position = spawnManager.spawnPoints[0].position;
        Hunted.position = spawnManager.spawnPoints[1].position;

    }
}
