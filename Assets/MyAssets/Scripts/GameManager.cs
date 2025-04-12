using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool GameStarted { get; private set; } = false;

    [SerializeField] private GameObject ExerciseProgram;
    private GameObject ProgramUI;
    private RehabProgram _RehabProgram;

    void Awake()
    {
        Instance = this;
    }

    //called by host through canvas
    public void StartGame()
    {
        PlayerManager.instance.SetPlayerPositions();
        //PlayerManager.instance.InitiateHomePositions();
        //BallManager.instance.SetupBall();

        ProgramUI.SetActive(false);
        _RehabProgram = ExerciseProgram.GetComponentInChildren<RehabProgram>();

        //handoff ownership of rehabProgram to client
        ulong clientId = Array.Find<ulong>(PlayerManager.instance.playerClientIds, id => id != 0);
        ExerciseProgram.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        _RehabProgram.InitiateExercisesRpc();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(!NetworkManager.Singleton.IsHost)
        {
            gameObject.SetActive(false);
            return;
        }

        ExerciseProgram = Instantiate(ExerciseProgram);
        ExerciseProgram.GetComponent<NetworkObject>().Spawn(); //it is spawned

        ProgramUI = ExerciseProgram.transform.GetComponentInChildren<Canvas>(true).gameObject;
        ProgramUI.SetActive(true);
        Camera camera = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().transform.Find("XR-Player").GetComponent<XRRigReferences>().Camera.GetComponent<Camera>();
        ExerciseProgram.GetComponentInChildren<Canvas>().worldCamera = camera;
    }
}
