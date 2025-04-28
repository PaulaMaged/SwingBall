using com.rfilkov.components;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PlayerNetworkManager : NetworkBehaviour
{
    public Camera mainCamera;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            this.enabled = false;
            return;
        }

        Transform spawnPoint = GameManager.Instance.SpawnPoint;
        //sets this player's transform

        //by default, all components for player movement are off for reasons beyond this comment
        EnablePlayerMovement();
        var netObjId = GetComponent<NetworkObject>().NetworkObjectId;

        UpdatePlayerListRpc(netObjId);

        SetPlayerTransformData(spawnPoint.position, spawnPoint.rotation);
        //Ensure player's camera shows on screen
        mainCamera.depth = 5;
    }

    public void EnablePlayerMovement()
    {
        TrackedPoseDriver[] trackedPoseDrivers = GetComponentsInChildren<TrackedPoseDriver>();
        foreach (TrackedPoseDriver trackedPoseDriver in trackedPoseDrivers)
        {
            trackedPoseDriver.enabled = true;
        }

        GetComponent<InputActionManager>().enabled = true;
        GetComponent<AvatarController>().enabled = true;
    }

    [Rpc(SendTo.Server)]
    void UpdatePlayerListRpc(ulong playerNetworkObjectId)
    {
        StartCoroutine(PlayerManager.instance.AddPlayer(playerNetworkObjectId));
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerPositionAndRotationRpc(Vector3 newPosition, Quaternion newRotation = default)
    {
        Debug.Log($"Our Lovely Quaternion {newRotation}");
        SetPlayerTransformData(newPosition, newRotation);
    }

    private void SetPlayerTransformData(Vector3 newPosition, Quaternion? newRotation = null)
    {
        transform.position = newPosition;

        if (newRotation == null)
        {
            Transform playerTransform = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().transform;
            Vector3 polePosition = BallManager.instance.poleTransform.position;
            Vector3 newRotationDirection = polePosition - playerTransform.position;
            newRotationDirection.y = 0; //look parallel to surface
            newRotation = Quaternion.LookRotation(newRotationDirection);
            playerTransform.rotation = newRotation.Value;
        }

        transform.rotation = newRotation.Value;
    }
}