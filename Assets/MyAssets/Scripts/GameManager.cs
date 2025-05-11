using com.rfilkov.kinect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool GameStarted { get; private set; } = false;

    [SerializeField] private GameObject ExerciseProgram;
    public Transform SpawnPoint;

    private GameObject ProgramUI;
    private RehabProgram _RehabProgram;

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

        //handoff ownership of rehabProgram to client
        ulong clientId = Array.Find<ulong>(PlayerManager.instance.PlayerClientIds, id => id != 0);
        ExerciseProgram.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        _RehabProgram.SyncExerciseConfigurationRpc(_RehabProgram.GetExerciseConfigurations());
        _RehabProgram.InitiateExercisesRpc();
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
                playerNetworkManagers[i].InitNetClient(i);
            }
        }

        KinectManager.Instance.ResetSensors();

        playerNetworkManagers[localPlayerIndex].InitKinectServer(localPlayerIndex);
    }
}
