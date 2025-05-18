using com.rfilkov.components;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager instance;


    public int distanceToPole = 5;

    public GameObject[] Players { get; private set; }
    public ulong[] PlayerClientIds { get; private set; }
    public GameObject[] Rackets { get; private set; }
    public Vector3[] PlayerPositions { get; private set; }

    private int playerCount = 0;
    private int maximumPlayerCount = 2;
    private Vector3[] homePositions;

    public NetworkList<NetworkObjectReference> PlayerNetworkObjectRefs { get; private set; }
    private NetworkVariable<int> currentPlayerTurnIndex = new(0);

    [SerializeField] private Vector3 pointCloudGroundOffset = new(0, 1, 0);
    [SerializeField] private Vector3 pointCloudsideOffset = new(1.5f, 0, 0);
    [SerializeField] private bool changeLocalPlayerRepresentation = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        PlayerNetworkObjectRefs = new();

        PlayerPositions = new Vector3[maximumPlayerCount];
        SetPlayerPositions();

        Players = new GameObject[maximumPlayerCount];
        PlayerClientIds = new ulong[maximumPlayerCount];

        homePositions = new Vector3[maximumPlayerCount];
        Rackets = new GameObject[maximumPlayerCount];
    }
    public void SetPlayerPositions()
    {
        //use unit circle to set player positions
        int angleIncrement = 360 / maximumPlayerCount;
        int currentAngle = 0;
        int radius = distanceToPole;

        Vector3 startPosition = BallManager.instance.poleTransform.position;

        for (int i = 0; i < PlayerPositions.Length; i++)
        {
            float angle = (currentAngle + i * angleIncrement) * Mathf.Deg2Rad;
            float positionX = (Mathf.Cos(angle) * radius) + startPosition.x;
            float positionZ = (Mathf.Sin(angle) * radius) + startPosition.z;

            PlayerPositions[i] = new Vector3(positionX, 0, positionZ);
        }
    }

    public Vector3 GetBallAnchorPositionForLocalPlayer()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        int playerIndex = Array.FindIndex<ulong>(PlayerClientIds, playerClientId => playerClientId == localClientId);
        return Rackets[playerIndex].GetNamedChild("BallAnchor").transform.position;
    }

    [Rpc(SendTo.Server)]
    public void SetBallHomePositionRpc(ulong clientId)
    {
        int playerIndex = Array.FindIndex<ulong>(PlayerClientIds, playerClientId => playerClientId == clientId);
        Debug.Log($"The Player Index to set ball home position at is: {playerIndex}");
        homePositions[playerIndex] = Rackets[playerIndex].GetNamedChild("BallAnchor").transform.position;
    }

    [Rpc(SendTo.NotServer)]
    public void SetupPlayerFieldsOnClientRpc()
    {
        foreach (NetworkObjectReference playerNetworkObjectRef in PlayerNetworkObjectRefs)
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
        Debug.Log($"Can Game start? : {(IsAllHomePositionsSet ? "Yes" : "No")}");
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

        if (IsServer) PlayerNetworkObjectRefs.Add(new NetworkObjectReference(playerNetworkObject));

        PlayerClientIds[playerCount] = playerNetworkObject.OwnerClientId;
        Players[playerCount] = player;
        Rackets[playerCount] = Players[playerCount].GetNamedChild("RBat");

        playerCount++;
    }

    public GameObject GetCurrentPlayer()
    {
        return Players[currentPlayerTurnIndex.Value];
    }

    public GameObject GetNextPlayer()
    {
        return Players[(currentPlayerTurnIndex.Value + 1) % playerCount];
    }

    [Rpc(SendTo.Server)]
    public void SwitchTurnRpc()
    {
        currentPlayerTurnIndex.Value = (currentPlayerTurnIndex.Value + 1) % Players.Length;
    }

    public Vector3 GetCurrentPlayerBallHomePosition()
    {
        return homePositions[currentPlayerTurnIndex.Value];
    }

    public void PlacePlayers()
    {
        int i = 0;
        foreach (var player in Players)
        {
            if (player == null) continue;

            PlayerNetworkManager playerNetworkManager = player.GetComponent<PlayerNetworkManager>();

            playerNetworkManager.UpdatePlayerPositionAndRotationRpc(PlayerPositions[i++]);
        }
    }

    private void EnableSkinnedMeshRenderers(GameObject player, bool enable)
    {

        SkinnedMeshRenderer[] skinnedMeshRenderers = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            skinnedMeshRenderer.enabled = enable;
        }
    }

    private void EnablePointCloud(GameObject player, bool enable, Vector3 pointCloudOffset)
    {
        UserMeshRendererGpu[] userMeshRenderers = player.GetComponentsInChildren<UserMeshRendererGpu>(true);
        foreach (UserMeshRendererGpu userMeshRenderer in userMeshRenderers)
        {
            //stop processing when false, the userMeshRendererGpu to limit processing
            userMeshRenderer.enabled = enable;
            userMeshRenderer.PointCloudOffset = pointCloudOffset;

            if (userMeshRenderer.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.enabled = enable;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SwitchRepresnetationRpc(bool enableHumanoid, bool enablePointCloud)
    {
        GameObject localPlayerGO = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
        Vector3 pointCloudOffset = enableHumanoid && enablePointCloud ? pointCloudGroundOffset + pointCloudsideOffset : pointCloudGroundOffset;

        foreach (GameObject player in Players)
        {
            if (player == null) continue;
            if (localPlayerGO == player && !changeLocalPlayerRepresentation) continue;

            EnableSkinnedMeshRenderers(player, enableHumanoid);
            EnablePointCloud(player, enablePointCloud, pointCloudOffset);
        }
    }
}