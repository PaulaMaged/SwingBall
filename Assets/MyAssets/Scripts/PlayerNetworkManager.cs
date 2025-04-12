using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class PlayerNetworkManager : NetworkBehaviour
{
    public XRDeviceSimulator XRDeviceSimulator;
    public Camera mainCamera;
    void Start()
    {
        var netObjId = GetComponent<NetworkObject>().NetworkObjectId;
        UpdatePlayerListServerRpc(netObjId);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            this.enabled = false;

            XRDeviceSimulator.enabled = false; //testing XR Simulator
            return;
        }

        //Ensure player's camera shows on screen
        mainCamera.depth = 5;
    }

    [ServerRpc]
    void UpdatePlayerListServerRpc(ulong playerNetworkObjectId)
    {
        PlayerManager.instance.AddPlayer(playerNetworkObjectId);
    }

    [ClientRpc]
    public void UpdatePlayerPositionClientRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
