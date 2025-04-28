using com.rfilkov.components;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PlayerNetworkManager : NetworkBehaviour
{
    public Camera mainCamera;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        //sets this player's transform
        Transform spawnPoint = GameManager.Instance.SpawnPoint;
        SetPlayerTransformData(spawnPoint.position, spawnPoint.rotation);

        //Func<Vector3, Quaternion, bool> IsTransformChanged = (oldPosition, oldRotation) =>
        //{
        //    if (transform.position != oldPosition || transform.rotation != oldRotation)
        //        return true;
        //    else
        //        return false;
        //};

        //StartCoroutine(ListenToTransformCoroutine(IsTransformChanged, spawnPoint.position, spawnPoint.rotation));

        //by default, all components for player movement are off for reasons beyond this comment
        EnablePlayerMovement();
        var netObjId = GetComponent<NetworkObject>().NetworkObjectId;

        UpdatePlayerListRpc(netObjId);

        //Ensure player's camera shows on screen
        mainCamera.depth = 5;
    }

    private IEnumerator ListenToTransformCoroutine(Func<Vector3, Quaternion, bool> Predicate, Vector3 oldPosition, Quaternion oldRotation)
    {
        while(!Predicate(oldPosition, oldRotation)) {
            yield return null;
        }

        Debug.Log($"New Position: {transform.position} And Rotation: {transform.rotation}");
    }

    public void EnablePlayerMovement()
    {
        TrackedPoseDriver[] trackedPoseDrivers = GetComponentsInChildren<TrackedPoseDriver>();
        foreach (TrackedPoseDriver trackedPoseDriver in trackedPoseDrivers)
        {
            trackedPoseDriver.enabled = true;
        }

        GetComponent<InputActionManager>().enabled = true;

        if (TryGetComponent(out AvatarController avatarController)) {
            avatarController.enabled = true;
        }
    }

    [Rpc(SendTo.Server)]
    void UpdatePlayerListRpc(ulong playerNetworkObjectId)
    {
        StartCoroutine(PlayerManager.instance.AddPlayer(playerNetworkObjectId));
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerPositionAndRotationRpc(Vector3 newPosition, Quaternion newRotation = default)
    {
        bool equalityCheck = IsQuaternionDefault(newRotation);
        Debug.Log($"New Rotation: {newRotation}, Is Default? {equalityCheck}");
        SetPlayerTransformData(newPosition, equalityCheck? null : newRotation);
    }

    public bool IsQuaternionDefault(Quaternion q1)
    {
        Quaternion q2 = default;
        for(int i = 0; i < 4; i++)
        {
            if(Mathf.Abs(q1[i] - q2[i]) > 0.0001)
            {
                return false;
            }
        }

        return true;
    }

    private void SetPlayerTransformData(Vector3 newPosition, Quaternion? newRotation = null)
    {
        transform.position = newPosition;

        if (newRotation == null)
        {
            Vector3 polePosition = BallManager.instance.poleTransform.position;
            Vector3 newRotationDirection = polePosition - transform.position;
            newRotationDirection.y = 0; //look parallel to surface
            newRotation = Quaternion.LookRotation(newRotationDirection);
        }

        transform.rotation = newRotation.Value;
    }
}