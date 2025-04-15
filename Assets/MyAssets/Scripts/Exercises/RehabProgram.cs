using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;
using System;
using Unity.Collections;
using UnityEngine.TextCore.LowLevel;
using UnityEditor.Experimental.GraphView;
using UnityEngine.InputSystem;
using Unity.Android.Gradle.Manifest;

public class RehabProgram : NetworkBehaviour
{
    public static RehabProgram Instance;
    public List<Exercise> Exercises;
    public GameObject ExerciseEntryPrefab;
    public Transform contentParent;
    public GameObject ProgressCanvasPrefab;
    private GameObject ProgressCanvasInstance;

    [SerializeField] private GameObject ReferenceCharacterPrefab;
    private GameObject ReferenceCharacterInstance;
    private Animator ReferenceCharacterAnimator;
    
    private Dictionary<string, TMP_Text> FieldNameReferencePair = new();
    private NetworkVariable<ExerciseProgress> exerciseProgress = new(writePerm: NetworkVariableWritePermission.Owner);

    public bool IsPatient { get; private set; } = true;

    private int currentExerciseIndex = -1;

    [SerializeField] private InputActionReference StartExerciseButton;
    [SerializeField] private InputActionReference PoseConfirmationButton;

    public void Awake()
    {
        Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsHost) return;

        IsPatient = false;
        exerciseProgress.Value = new ExerciseProgress(currentExerciseIndex);
        ConfigureExercises();
    }

    private void ConfigureExercises()
    {
        foreach (Exercise exercise in Exercises)
        {
            GameObject entry = Instantiate(ExerciseEntryPrefab, contentParent);

            //replace with exercise name
            entry.transform.Find("Exercise Name").GetComponent<TMP_Text>().text = exercise.Name;

            ConfigureSlider(
                parent: entry.transform.Find("Sets").gameObject,
                initialValue: exercise.Sets,
                onValueChanged: (newValue) => exercise.Sets = (int)newValue
                );
            
            ConfigureSlider(
                parent: entry.transform.Find("Reps").gameObject,
                initialValue: exercise.Reps,
                onValueChanged: (newValue) => exercise.Reps = (int)newValue
                );

            ConfigureSlider(
                parent: entry.transform.Find("BreakTime").gameObject,
                initialValue: exercise.breakTimeSeconds,
                onValueChanged: (newValue) => exercise.breakTimeSeconds = (int)newValue
                );
        }
    }

    private void ConfigureSlider(GameObject parent, int initialValue, Action<float> onValueChanged)
    {
        TMP_Text textComponent = parent.transform.Find("value").GetComponent<TMP_Text>();


        textComponent.text = initialValue.ToString();

        Slider valueSlider = parent.transform.Find("Slider").GetComponent<Slider>();
        valueSlider.value = initialValue;

        valueSlider.onValueChanged.AddListener((newValue) =>
        {
            onValueChanged(newValue);
            textComponent.text = newValue.ToString();
        });

    }

    public bool IsProgramCompleted() {
        if (currentExerciseIndex >= Exercises.Count)
            return true;
        else
            return false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitiateExercisesRpc()
    {
        //setup canvas for stats, accuracy & character motion preview
        Vector3 canvasPosition = BallManager.instance.poleTransform.position + Vector3.forward * 10 + Vector3.up * 4;
        Quaternion canvasRotation = Quaternion.identity;
        ProgressCanvasInstance = Instantiate(ProgressCanvasPrefab, canvasPosition, canvasRotation);
        ReferenceCharacterInstance = Instantiate(ReferenceCharacterPrefab);
        ReferenceCharacterAnimator = ReferenceCharacterInstance.GetComponent<Animator>();
        CacheUIFields();
        exerciseProgress.OnValueChanged += UpdateProgressUI;
        PoseConfirmationButton.action.started += SetupAnchorPoint;

        if (!IsOwner) return;
        SetupNextExercise();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void PlayReferenceCharacterAnimationRpc()
    {
        ReferenceCharacterAnimator.Play(Exercises[currentExerciseIndex + 1].Name);
    }

    //called on owner side
    public void SetupNextExercise()
    {
        //increments index by one locally so that the networkvariable isn't waited for
        PlayReferenceCharacterAnimationRpc();

        currentExerciseIndex++;
        //link with canvas
        exerciseProgress.Value = new ExerciseProgress(currentExerciseIndex);

        //reset information from server side
        MoveToNewExerciseRpc();

        //indicator for what the current static pose required is and when it is matched
    }

    [Rpc(SendTo.Server)]
    public void MoveToNewExerciseRpc()
    {
        BallManager.instance.SetBallPositionToIdle();
        PlayerManager.instance.ClearBallHomePositions();
        StartExerciseButton.action.started += StartExercise;
    }

    public void StartExercise(InputAction.CallbackContext context)
    {
        StartExerciseRpc();
    }

    [Rpc(SendTo.Server)]
    public void StartExerciseRpc()
    {
        if (!PlayerManager.instance.IsAllBallHomePositionsSet()) return;

        BallManager.instance.SetupBall();
        StartExerciseButton.action.started -= StartExercise;
    }

    public void SetupAnchorPoint(InputAction.CallbackContext context)
    {
        Debug.Log($"Called Pose Confirmation Function with ClientId: {NetworkManager.Singleton.LocalClientId}");
        if (IsPatient && !PoseManager.instance.HasSatisfiedPoseAccuracy()) return; //for the local instance to find out the accuracy of its avatar

        PlayerManager.instance.SetBallHomePositionRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void CacheUIFields()
    {
        //add TMP to dictionary
        TMP_Text[] exerciseStatsFields = ProgressCanvasInstance.transform.Find("Values").GetComponentsInChildren<TMP_Text>();
        
        foreach(TMP_Text tmp in exerciseStatsFields)
        {
            string gameObjectName = tmp.gameObject.name;

            if(gameObjectName == "Reps")
            {
                FieldNameReferencePair.Add("Reps", tmp);
            } else if(gameObjectName == "Sets")
            {
                FieldNameReferencePair.Add("Sets", tmp);
            } else if(gameObjectName == "ExerciseName")
            {
                FieldNameReferencePair.Add("ExerciseName", tmp);
            } else if( gameObjectName == "BreakTime")
            {
                FieldNameReferencePair.Add("BreakTime", tmp);
            }
        }

    }
    public void UpdateProgressUI(ExerciseProgress previous, ExerciseProgress current)
    {
        TMP_Text tmp;

        FieldNameReferencePair.TryGetValue("Reps", out tmp);
        tmp.text = current.repsDone.ToString();

        FieldNameReferencePair.TryGetValue("Sets", out tmp);
        tmp.text = current.setsDone.ToString();

        FieldNameReferencePair.TryGetValue("ExerciseName", out tmp);
        tmp.text = Exercises[current.exerciseIndex].name;

        FieldNameReferencePair.TryGetValue("BreakTime", out tmp);
        tmp.text = current.breakTimeLeft.ToString();
    }

    public ExerciseConfiguration[] GetExerciseConfigurations()
    {
        ExerciseConfiguration[] exerciseConfigurations = new ExerciseConfiguration[Exercises.Count];
        for(int i = 0; i < Exercises.Count; i++)
        {
            Exercise exercise = Exercises[i];
            ExerciseConfiguration exerciseConfiguration = new(exercise);
            exerciseConfigurations[i] = exerciseConfiguration;
        }

        return exerciseConfigurations;
    }

    [Rpc(SendTo.Owner)] 
    public void SyncExerciseConfigurationRpc(ExerciseConfiguration[] exerciseConfigurations)
    {
        for(int i = 0; i < exerciseConfigurations.Length; i++) 
        {
            ExerciseConfiguration exerciseConfiguration = exerciseConfigurations[i];
            Exercise exercise = Exercises[i];

            exercise.Reps = exerciseConfiguration.RepCount;
            exercise.Sets = exerciseConfiguration.SetCount;
            exercise.breakTimeSeconds = exerciseConfiguration.BreakTimeSeconds;
        }
    }

    public struct ExerciseProgress : INetworkSerializable 
    {
        public int exerciseIndex;
        public int repsDone;
        public int setsDone;
        public int breakTimeLeft;

        public ExerciseProgress(int index)
        {
            exerciseIndex = index;
            repsDone = 0;
            setsDone = 0;
            breakTimeLeft = 0;
        }

        public void AddRep() { repsDone++; }

        public void AddSet() { setsDone++; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref exerciseIndex);
            serializer.SerializeValue(ref repsDone);
            serializer.SerializeValue(ref setsDone);
            serializer.SerializeValue(ref breakTimeLeft);
        }
    }

    public struct ExerciseConfiguration : INetworkSerializable
    {
        public int RepCount;
        public int SetCount;
        public int BreakTimeSeconds;

        public ExerciseConfiguration(Exercise exercise)
        {
            RepCount = exercise.Reps;
            SetCount = exercise.Sets;
            BreakTimeSeconds = exercise.breakTimeSeconds;
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RepCount);
            serializer.SerializeValue(ref SetCount);
            serializer.SerializeValue(ref BreakTimeSeconds);
        }
    }
}