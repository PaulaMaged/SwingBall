using Mono.Cecil;
using System;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
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

        for(int i = 0; i < playerPositions.Length; i++)
        {
            float angle = (currentAngle + i * angleIncrement) * Mathf.Deg2Rad;
            float positionX = (Mathf.Cos(angle) * radius) + startPosition.x;
            float positionZ = (Mathf.Sin(angle) * radius) + startPosition.z;

            playerPositions[i] = new Vector3(positionX, 0, positionZ);
        }
    }

    public Vector3 GetBallAnchorPositionForLocalPlayer()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        int playerIndex = Array.FindIndex<ulong>(playerClientIds, playerClientId => playerClientId == localClientId);
        return Rackets[playerIndex].GetNamedChild("BallAnchor").transform.position;
    }

    [Rpc(SendTo.Server)]
    public void SetBallHomePositionRpc(ulong clientId)
    {
        int playerIndex = Array.FindIndex<ulong>(playerClientIds, playerClientId => playerClientId == clientId);
        Debug.Log($"The Player Index to set ball home position at is: {playerIndex}");
        homePositions[playerIndex] = Rackets[playerIndex].GetNamedChild("BallAnchor").transform.position;
    }

    public bool IsAllBallHomePositionsSet()
    {
        bool IsAllHomePositionsSet = homePositions.All(homePosition => homePosition != default);
        Debug.Log($"Can Game start? : {(IsAllHomePositionsSet? "Yes" : "No")}");
        return IsAllHomePositionsSet;
    }

    public void ClearBallHomePositions()
    {
        homePositions.Initialize();
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
        Rackets[playerCount] = players[playerCount].GetNamedChild("RBat");

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

    [Rpc(SendTo.ClientsAndHost)]
    public void SwitchTurnRpc()
    {
        if(RehabProgram.Instance.IsPatient)
        {

        }

        // Disable current bats's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = false;
        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;
        // Enable new current player's collider
        Rackets[currentPlayerIndex].GetComponent<Collider>().enabled = true;
    }

    public Vector3 GetCurrentPlayerBallHomePosition()
    {
        return homePositions[currentPlayerIndex];
    }

    public void SetPlayerPositions()
    {
        int i = 0;
        foreach(var player in players)
        {
            if (player == null) continue;

            PlayerNetworkManager playerNetworkManager = player.GetComponent<PlayerNetworkManager>();

            playerNetworkManager.UpdatePlayerPositionAndRotationRpc(playerPositions[i++]);
        }
    }
}