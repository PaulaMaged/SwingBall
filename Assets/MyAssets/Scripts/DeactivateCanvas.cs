using UnityEngine;
using Unity.Netcode;

public class DeactivateCanvas : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) gameObject.SetActive(false);
    }
}
