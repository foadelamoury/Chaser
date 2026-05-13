using System;
using Unity.Netcode;
using UnityEngine;

public class TheCollider : NetworkBehaviour, IDamageable
{
    NetworkVariable<float> Health = new NetworkVariable<float>(100);
    SpawnManager spawnManager;

    void Awake()
    {
         spawnManager = FindAnyObjectByType<SpawnManager>();
    }

    public void Die(Collision2D collision)
    {
        GameObject collider = collision.gameObject;        
        if (gameObject.name.Contains("Hunted") && collider.name.Contains("Hunter"))
        {
             GameManager.Instance.EndGame("Police wins");
            GameManager.Instance.timerText.gameObject.SetActive(false);
        GameManager.Instance.UpdateScoreServerRpc(0); // Police wins
        GameManager.Instance.ResetPosition(transform,collider.transform);


        }
    }

  

    public void TakeDamage(float damage)
    {
        Health.Value -= damage;
    }





}
