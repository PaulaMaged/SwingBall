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

        //by default, all components for player movement are off for reasons beyond this comment
        EnablePlayerMovement();
        var netObjId = GetComponent<NetworkObject>().NetworkObjectId;
        UpdatePlayerListRpc(netObjId);

        //Ensure player's camera shows on screen
        mainCamera.depth = 5;
    }

    public void EnablePlayerMovement()
    {
        TrackedPoseDriver[] trackedPoseDrivers = GetComponentsInChildren<TrackedPoseDriver>();
        foreach(TrackedPoseDriver trackedPoseDriver in trackedPoseDrivers)
        {
            trackedPoseDriver.enabled = true;
        }

        GetComponentInChildren<InputActionManager>().enabled = true;
    }

    [Rpc(SendTo.Server)]
    void UpdatePlayerListRpc(ulong playerNetworkObjectId)
    {
        PlayerManager.instance.AddPlayer(playerNetworkObjectId);
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerPositionAndRotationRpc(Vector3 newPosition, Quaternion newRotation = default)
    {
        transform.position = newPosition;
        Vector3 newRotationDirection = BallManager.instance.poleTransform.position - NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().transform.position;
        newRotationDirection.y = 0; //look parallel to surface
        newRotation = Quaternion.LookRotation(newRotationDirection);
        transform.rotation = newRotation;
    }
}
