using System;
using Unity.Netcode;
using UnityEngine;

public class RacketBallInteraction : NetworkBehaviour
{
    public event Action<Collision> OnRacketBallCollision;

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;

        if (collision.gameObject.CompareTag("Ball"))
        {
            OnRacketBallCollision?.Invoke(collision);
        }
    }
}
