using com.rfilkov.components;
using com.rfilkov.kinect;
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
            InitNetClient();
            enabled = false;
            return;
        }

        InitNetServer();
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

    /// <summary>
    /// Instantiates a netclient with server base port reflecting the index that this player takes inside of the networked playernetworkobj refs
    /// The netclient is to be spawned on the kinectmanager's gameobject. The sensorindex passed in should be the count 
    /// </summary>
    private void InitNetClient()
    {
        int index = PlayerManager.instance.PlayerNetworkObjectRefs.IndexOf(new NetworkObjectReference(NetworkObject));
        if (index == -1) throw new Exception("Player network Object must exist inside of list");

        //create the netclient and open it
        GameObject go = new("netClient");
        NetClientInterface netClientTemplate = go.AddComponent<NetClientInterface>();

        int serverBasePort = 10000 + 1000 * index;
        netClientTemplate.deviceIndex = index;
        netClientTemplate.serverBasePort = serverBasePort;
        
        Instantiate(go, KinectManager.Instance.transform);
        KinectManager.Instance.ResetSensors(); //this is enough to initiate the client listening

        UserMeshRendererGpu userMeshRendererGpu = GetComponentInChildren<UserMeshRendererGpu>(true);
        userMeshRendererGpu.sensorIndex = index;
        userMeshRendererGpu.enabled = true;
    }

    private void InitNetServer()
    {
        int index = PlayerManager.instance.PlayerNetworkObjectRefs.Count;

        //create the netServer and k4a instance
        GameObject go = new("Kinect4AzureInterface");
        Kinect4AzureInterface k4aIntTemplate = go.AddComponent<Kinect4AzureInterface>();
        KinectNetServer kinectNetServerTemplate = go.AddComponent<KinectNetServer>();

        k4aIntTemplate.deviceIndex = index;

        int serverBasePort = 10000 + 1000 * index;
        kinectNetServerTemplate.sensorIndex = index;
        kinectNetServerTemplate.baseListenPort = serverBasePort;

        KinectNetServer kinectNetServerInstance  = Instantiate(kinectNetServerTemplate, KinectManager.Instance.transform);

        AvatarControllerV2 avatarControllerV2 = GetComponentInChildren<AvatarControllerV2>(true);
        avatarControllerV2.SensorIndex = index;

        KinectManager.Instance.ResetSensors();

        UserMeshRendererGpu userMeshRendererGpu = GetComponentInChildren<UserMeshRendererGpu>(true);
        userMeshRendererGpu.sensorIndex = index;
        userMeshRendererGpu.enabled = true;

        kinectNetServerInstance.StartServer(serverBasePort);
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