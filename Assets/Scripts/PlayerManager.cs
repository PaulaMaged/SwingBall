using System.Linq;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public GameObject[] players;
    private int currentPlayerIndex = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        players = GameObject.FindGameObjectsWithTag("Racket");
    }

    public GameObject GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public GameObject GetNextPlayer()
    {
        return players[(currentPlayerIndex + 1) % players.Length];
    }

    public void SwitchTurn()
    {
        // Disable current player's collider
        players[currentPlayerIndex].GetComponent<Collider>().enabled = false;
        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;
        // Enable new current player's collider
        players[currentPlayerIndex].GetComponent<Collider>().enabled = true;
    }
}
