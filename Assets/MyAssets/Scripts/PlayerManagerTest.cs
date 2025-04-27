using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerManagerTest : MonoBehaviour
{
    public static PlayerManagerTest instance;
    public GameObject[] Rackets { get; private set; }
    private Vector3[] homePositions;
    private int currentPlayerIndex = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        Rackets = GameObject.FindGameObjectsWithTag("Racket");
        homePositions = new Vector3[Rackets.Length];
    }

    public void InitiateHomePositions()
    {
        for (int i = 0; i < homePositions.Length; i++)
        {
            homePositions[i] = Rackets[i].GetNamedChild("BallAnchor").transform.position;
            Debug.Log("Home Position " + i + ": " + homePositions[i]);
        }
    }

    public GameObject GetCurrentPlayer()
    {
        return Rackets[currentPlayerIndex];
    }

    public GameObject GetNextPlayer()
    {
        return Rackets[(currentPlayerIndex + 1) % Rackets.Length];
    }

    public void SwitchTurn()
    {
        // Disable current bats's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = false;
        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % Rackets.Length;
        // Enable new current player's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = true;
    }

    public Vector3 GetCurrentPlayerHomePosition()
    {
        //Debug statement
        Vector3 currentPosition = Rackets[currentPlayerIndex].GetNamedChild("BallAnchor").transform.position;

        return homePositions[currentPlayerIndex];
    }


}