using Mono.Cecil;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager instance;
    public NetworkList<NetworkObjectReference> playerNetworkObjectRefs;
    public NetworkVariable<int> currentPlayerTurnIndex = new(0);

    public GameObject[] players;
    public ulong[] playerClientIds;
    public GameObject[] Rackets { get; private set; }
    private int playerCount = 0;
    private int maximumPlayerCount = 2;
    private Vector3[] homePositions;
    public Vector3[] playerPositions;
    public int distanceToPole = 5;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        playerNetworkObjectRefs = new();

        playerPositions = new Vector3[maximumPlayerCount];
        SetPlayerPositions();

        players = new GameObject[maximumPlayerCount];
        playerClientIds = new ulong[maximumPlayerCount];

        homePositions = new Vector3[maximumPlayerCount];
        Rackets = new GameObject[maximumPlayerCount];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsServer)
        {
            playerNetworkObjectRefs.Clear();
            Debug.Log("Server has cleared player network refs");
        }
    }
    public void SetPlayerPositions()
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

    [Rpc(SendTo.NotServer)]
    public void SetupPlayerFieldsOnClientRpc()
    {
        foreach (NetworkObjectReference playerNetworkObjectRef in playerNetworkObjectRefs)
        {
            if (playerNetworkObjectRef.TryGet(out NetworkObject playerNetworkObject))
            {
                StartCoroutine(AddPlayer(playerNetworkObject.NetworkObjectId));
            }
        }
    }

    public bool IsAllBallHomePositionsSet()
    {
        bool IsAllHomePositionsSet = homePositions.All(homePosition => homePosition != default);
        Debug.Log($"Can Game start? : {(IsAllHomePositionsSet? "Yes" : "No")}");
        return IsAllHomePositionsSet;
    }

    public void ClearBallHomePositions()
    {
        homePositions = new Vector3[playerCount];
    }

    public IEnumerator AddPlayer(ulong playerNetworkObjectId)
    {
        while (!IsSpawned) yield return null;

        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerNetworkObjectId];
        GameObject player = playerNetworkObject.gameObject;

        if (playerCount == maximumPlayerCount)
        {
            Debug.Log("maximum player count reached");
            yield break;
        }

        if(IsServer) playerNetworkObjectRefs.Add(new NetworkObjectReference(playerNetworkObject));

        playerClientIds[playerCount] = playerNetworkObject.OwnerClientId;
        players[playerCount] = player;
        Rackets[playerCount] = players[playerCount].GetNamedChild("RBat");

        playerCount++;
    }

    public GameObject GetCurrentPlayer()
    {
        return players[currentPlayerTurnIndex.Value];
    }

    public GameObject GetNextPlayer()
    {
        return players[(currentPlayerTurnIndex.Value + 1) % playerCount];
    }

    [Rpc(SendTo.Server)]
    public void SwitchTurnRpc()
    {
        currentPlayerTurnIndex.Value = (currentPlayerTurnIndex.Value + 1) % players.Length;
    }

    public Vector3 GetCurrentPlayerBallHomePosition()
    {
        return homePositions[currentPlayerTurnIndex.Value];
    }

    public void PlacePlayers()
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