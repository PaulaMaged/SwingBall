using com.rfilkov.components;
using com.rfilkov.kinect;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Collections;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PlayerNetworkManager : NetworkBehaviour
{
    public Camera mainCamera;

    private const bool DEBUGGING = true;

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

        //by default, all components for player movement are off for reasons beyond this comment
        EnablePlayerMovement();
        var netObjId = GetComponent<NetworkObject>().NetworkObjectId;

        UpdatePlayerListRpc(netObjId);

        //Ensure player's camera shows on screen
        mainCamera.depth = 5;
    }

    /// <summary>
    /// Instantiates a netclient with server base port reflecting the index that this player takes inside of the networked playernetworkobj refs
    /// The netclient is to be spawned on the kinectmanager's gameobject.
    /// </summary>
    public void InitNetClient(int index)
    {
        Debug.Log($"Creating a netclient for player with network object refs index: {index}");
        if (index == -1) throw new Exception("Player network Object must exist inside of list");

        //create the netclient and open it
        GameObject go = new("netClient");
        go.transform.SetParent(KinectManager.Instance.transform);
        NetClientInterface netClient = go.AddComponent<NetClientInterface>();

        int serverBasePort = 10000 + 1000 * index;
        netClient.sensorPriority = index;
        netClient.serverBasePort = serverBasePort;
        netClient.autoServerDiscovery = true;

        SetupKinectTracking(index);
    }

    public void InitLocalSenosr(int index)
    {
        //create the netServer and k4a instance
        GameObject go = new("Kinect4AzureInterface");
        go.transform.SetParent(KinectManager.Instance.transform);
        Kinect4AzureInterface k4aInt = go.AddComponent<Kinect4AzureInterface>();

        if (DEBUGGING)
        {
            k4aInt.deviceStreamingMode = KinectInterop.DeviceStreamingMode.PlayRecording;
            k4aInt.recordingFile = IsHost ? "./Recordings/me.mkv" : "./Recordings/him.mkv";
            k4aInt.loopPlayback = true;
        }

        k4aInt.bodyTrackingProcessingMode = SystemInfo.graphicsDeviceVendor.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ?
            k4abt_tracker_processing_mode_t.K4ABT_TRACKER_PROCESSING_MODE_GPU_CUDA :
            k4abt_tracker_processing_mode_t.K4ABT_TRACKER_PROCESSING_MODE_GPU_DIRECTML;


        k4aInt.sensorPriority = index;

        SetupKinectTracking(index);
    }

    public void InitKinectServer(int index)
    {
        GameObject go = new("KinectNetServer");
        KinectNetServer kinectNetServer = go.AddComponent<KinectNetServer>();

        int serverBasePort = 10000 + 1000 * index;
        kinectNetServer.sensorIndex = index;
        kinectNetServer.baseListenPort = serverBasePort;
        kinectNetServer.listenForServerDiscovery = true;

        kinectNetServer.StartServer();
    }

    private void SetupKinectTracking(int index)
    {
        AvatarControllerV2 avatarControllerV2 = GetComponentInChildren<AvatarControllerV2>(true);
        avatarControllerV2.SensorIndex = index;
        avatarControllerV2.enabled = true;

        UserMeshRendererGpu userMeshRendererGpu = GetComponentInChildren<UserMeshRendererGpu>(true);
        userMeshRendererGpu.sensorIndex = index;
    }

    public void EnablePlayerMovement()
    {
        TrackedPoseDriver[] trackedPoseDrivers = GetComponentsInChildren<TrackedPoseDriver>(true);
        foreach (TrackedPoseDriver trackedPoseDriver in trackedPoseDrivers)
        {
            trackedPoseDriver.enabled = true;
        }

        GetComponent<InputActionManager>().enabled = true;
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
        if (newRotation == null)
        {
            Vector3 polePosition = BallManager.instance.poleTransform.position;
            Vector3 newRotationDirection = polePosition - newPosition;
            newRotationDirection.y = 0; // look parallel to surface
            newRotation = Quaternion.LookRotation(newRotationDirection);
        }

        if (TryGetComponent<AvatarControllerV2>(out var avatarController))
        {
            Debug.Log("Update Player Position");
            avatarController.ResetInitialTransform(newPosition, newRotation.Value);
        }
    }
}