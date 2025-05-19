using com.rfilkov.kinect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool GameStarted { get; private set; } = false;

    [SerializeField] private GameObject ExerciseProgram;
    public Transform SpawnPoint;

    private GameObject ProgramUI;
    private RehabProgram _RehabProgram;

    [Tooltip("Choose which one to show on startup")]
    [SerializeField] private Representation currentRepresentation = Representation.Both;
    [SerializeField] private InputActionReference switchRepresentationButton;

    public UnitUI panelPrefab;
    public float spreadAngle = 30f;
    public float distance = 2f;

    void Awake()
    {
        Instance = this;
    }

    //called by host through canvas
    public void StartGame()
    {
        PlayerManager.instance.SetupPlayerFieldsOnClientRpc();
        PlayerManager.instance.PlacePlayers();
        GameStarted = true;
        ProgramUI.SetActive(false);
        _RehabProgram = ExerciseProgram.GetComponentInChildren<RehabProgram>();

        SetupSensorsRpc();

        SetupRepresentationChangeButton();

        //handoff ownership of rehabProgram to client
        ulong clientId = Array.Find<ulong>(PlayerManager.instance.PlayerClientIds, id => id != 0);
        ExerciseProgram.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        _RehabProgram.SyncExerciseConfigurationRpc(_RehabProgram.GetExerciseConfigurations());
        _RehabProgram.InitiateExercisesRpc();
    }

    public void SetupRepresentationChangeButton()
    {
        switchRepresentationButton.action.started += SwitchRepresentationButtonHandler;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        ExerciseProgram = Instantiate(ExerciseProgram);
        ExerciseProgram.GetComponent<NetworkObject>().Spawn(); //it is spawned

        ProgramUI = ExerciseProgram.transform.GetComponentInChildren<Canvas>(true).gameObject;
        ProgramUI.SetActive(true);
        Camera camera = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().transform.GetComponent<XRRigReferences>().Camera.GetComponent<Camera>();
        ExerciseProgram.GetComponentInChildren<Canvas>().worldCamera = camera;
    }

    [Rpc(SendTo.Everyone)]
    private void SetupSensorsRpc()
    {
        NetClientInterface netClient = null;
        KinectNetServer netServer = null;
        KinectManager kinectManager = KinectManager.Instance;

        int localPlayerIndex = -1;

        PlayerNetworkManager[] playerNetworkManagers = null;

        IReadOnlyList<NetworkObject> playerNetworkObjects = NetworkManager.SpawnManager.PlayerObjects;

        playerNetworkManagers = playerNetworkObjects.Select((playerNetObj, index) => {
            if (playerNetObj.IsLocalPlayer) localPlayerIndex = index; //sets localPlayerIndex
                
            return playerNetObj.GetComponentInChildren<PlayerNetworkManager>(true);
            }).ToArray();

        for(int i = 0; i < playerNetworkManagers?.Length; i++)
        {
            if(i == localPlayerIndex)
            {
                playerNetworkManagers[i].InitLocalSenosr(localPlayerIndex);
            } else
            {
                netClient = playerNetworkManagers[i].InitNetClient(i);
            }
        }

        Stopwatch sw = Stopwatch.StartNew();
        KinectManager.Instance.StartDepthSensors();
        sw.Stop();

        UnityEngine.Debug.Log($"Time taken to start depth sensors: {sw.Elapsed.TotalSeconds}");

        netServer = playerNetworkManagers[localPlayerIndex].InitKinectServer(localPlayerIndex);

        if (kinectManager != null && netServer != null && netClient != null)
            SetupPanels(kinectManager, netServer, netClient);
        else
            UnityEngine.Debug.LogWarning("One of the the three essential components aren't initialized");

        SwitchRepresentation(currentRepresentation);
    }

    private void SetupPanels(KinectManager kinectManager, KinectNetServer netServer, NetClientInterface netClient)
    {
        List<UnitUI> panels = new();
        for (int i = 0; i < 3; i++)
        {
            float angle = ((float)i - (3 - 1) / 2f) * spreadAngle;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * -Camera.main.transform.forward;
            Vector3 pos = Camera.main.transform.position + dir.normalized * distance;

            UnitUI panel = Instantiate(panelPrefab, pos, Quaternion.identity);
            panel.canvas.worldCamera = Camera.main;
            
            // Make it face the camera
            panel.transform.LookAt(Camera.main.transform);
            panel.transform.Rotate(0, 180, 0); // because LookAt flips canvas back
    }

        kinectManager.ConsoleText = panels[0].consoleText;

        netServer.consoleText = panels[1].consoleText;
        netServer.connStatusText = panels[1].connStatusText;
        netServer.serverStatusText = panels[1].StatusText;

        netClient.consoleText = panels[2].consoleText;
        netClient.connStatusText = panels[2].connStatusText;
        netClient.ClientStatusText = panels[2].StatusText;
    }

    public void SwitchRepresentationButtonHandler(InputAction.CallbackContext ctx)
    {
        Representation nextRepresentation = (Representation)((int)(currentRepresentation + 1) % (int)Representation.Count);
        SwitchRepresentation(nextRepresentation);
        currentRepresentation = nextRepresentation;
    }

    public void SwitchRepresentation(Representation representation)
    {

        bool enableHumanoid = false;
        bool enablePointCloud = false;

        switch (representation)
        {
            case Representation.None:
                break;
            case Representation.Humanoid:
                enableHumanoid = true;
                break;
            case Representation.PointCloud:
                enablePointCloud = true;
                break;
            case Representation.Both:
                enableHumanoid = true;
                enablePointCloud = true;
                break;
        }

        PlayerManager.instance.SwitchRepresnetationRpc(enableHumanoid, enablePointCloud);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        switchRepresentationButton.action.started -= SwitchRepresentationButtonHandler;
    }
}
public enum Representation
{
    None,
    Humanoid,
    PointCloud,
    Both,
    Count
}