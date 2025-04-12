using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public GameObject[] players;
    public ulong[] playerClientIds;
    public GameObject[] Rackets { get; private set; }
    private int playerCount = 0;
    private int maximumPlayerCount = 2;
    private Vector3[] homePositions;
    private int currentPlayerIndex = 0;
    public Vector3[] playerPositions;
    public int distanceToPole = 5;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        playerPositions = new Vector3[maximumPlayerCount];
        SetPlayerPositions(playerPositions);

        players = new GameObject[maximumPlayerCount];
        playerClientIds = new ulong[maximumPlayerCount];

        homePositions = new Vector3[maximumPlayerCount];
        Rackets = new GameObject[maximumPlayerCount];
    }

    public void SetPlayerPositions(Vector3[] playerPositions)
    {
        //use unit circle to set player positions
        int angleIncrement = 360 / maximumPlayerCount;
        int currentAngle = 0;
        int radius = distanceToPole;

        Vector3 startPosition = BallManager.instance.poleTransform.position;
        Debug.Log($"offset position for players: {startPosition}");

        for(int i = 0; i < playerPositions.Length; i++)
        {
            float angle = (currentAngle + i * angleIncrement) * Mathf.Deg2Rad;
            float positionX = (Mathf.Cos(angle) * radius) + startPosition.x;
            float positionZ = (Mathf.Sin(angle) * radius) + startPosition.z;

            playerPositions[i] = new Vector3(positionX, 0, positionZ);
        }
    }

    public void InitiateHomePositions()
    {
        for (int i = 0; i < homePositions.Length; i++)
        {
            Rackets[i] = players[i].GetNamedChild("Bat");
            homePositions[i] = Rackets[i].GetNamedChild("BallAnchor").transform.position;
            Debug.Log("Home Position " + i + ": " + homePositions[i]);
        }
    }

    public bool AddPlayer(ulong playerNetworkObjectId)
    {
        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerNetworkObjectId];
        GameObject player = playerNetworkObject.gameObject;

        if (playerCount == maximumPlayerCount)
        {
            Debug.Log("maximum player count reached");
            return false;
        }
        playerClientIds[playerCount] = playerNetworkObject.OwnerClientId;
        players[playerCount] = player;
        playerCount++;
        return true;
    }

    public GameObject GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public GameObject GetNextPlayer()
    {
        return players[(currentPlayerIndex + 1) % playerCount];
    }

    public void SwitchTurn()
    {
        // Disable current bats's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = false;
        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;
        // Enable new current player's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = true;
    }

    public Vector3 GetCurrentPlayerHomePosition()
    {
        //Debug statement
        Vector3 currentPosition = Rackets[currentPlayerIndex].GetNamedChild("BallAnchor").transform.position;

        return homePositions[currentPlayerIndex];
    }

    public void SetPlayerPositions()
    {
        int i = 0;
        foreach(var player in players)
        {
            if (player == null) continue;

            PlayerNetworkManager playerNetworkManager = player.GetComponent<PlayerNetworkManager>();

            playerNetworkManager.UpdatePlayerPositionClientRpc(playerPositions[i++]);
        }
    }
}