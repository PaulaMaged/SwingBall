using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RehabProgram : NetworkBehaviour
{

    #region Variables
    public static RehabProgram Instance;
    public List<Exercise> Exercises;
    public GameObject ExerciseEntryPrefab;
    public Transform contentParent;
    public GameObject ProgressCanvasPrefab;
    private GameObject ProgressCanvasInstance;

    [SerializeField] private GameObject ReferenceCharacterPrefab;
    private GameObject ReferenceCharacterInstance;

    private Dictionary<string, TMP_Text> FieldNameReferencePair = new();
    public NetworkVariable<ExerciseProgress> exerciseProgress { get; private set; } = new(writePerm: NetworkVariableWritePermission.Owner);

    public bool IsPatient { get; private set; } = true;
    public bool IsBreakTime { get; private set; } = false;

    private int currentExerciseIndex = -1;

    [SerializeField] private InputActionReference StartExerciseButton;
    [SerializeField] private InputActionReference PoseConfirmationButton;

    #endregion

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

    public bool IsProgramCompleted()
    {
        if (currentExerciseIndex == Exercises.Count)
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
        PoseManager.instance.SetAnimator(ReferenceCharacterInstance.GetComponent<Animator>());

        CacheUIFields();

        exerciseProgress.OnValueChanged += UpdateProgressUI;
        PoseConfirmationButton.action.started += SetupAnchorPoint;

        if (!IsOwner) return;

        currentExerciseIndex = 0;
        SetupNextExercise();

    }

    //conditional statements for context checking
    [Rpc(SendTo.Owner)]
    public void OnMovementExecutionRpc()
    {
        if (IsBreakTime)
        {
            Debug.Log("No reps count during break");
            return;
        }

        if (!PoseManager.instance.HasCompletedMotion()) return;

        HandleRep();
        PoseManager.instance.NextPose();
    }

    //responsible for arbitrating the side effects of a rep
    public void HandleRep()
    {
        exerciseProgress.Value = exerciseProgress.Value.AddRep();

        bool FinishedSet = false;

        if (IsRepCountReached())
        {
            exerciseProgress.Value = exerciseProgress.Value.AddSet();
            FinishedSet = true;
        }

        if (IsSetCountReached())
        {
            OnExerciseFinished();
            return;
        }

        if (FinishedSet)
        {
            exerciseProgress.Value = exerciseProgress.Value.AddBreak();
            StartCoroutine(InitiateBreakTime());
        }
    }

    private void OnExerciseFinished()
    {
        currentExerciseIndex++;

        //reset information from server side
        SetGameToIdleRpc();

        if (!IsProgramCompleted())
        {
            SetupNextExercise();
        }
        else
        {
            //Call method responsible for finishing up the game
            Debug.Log("Game Finished");
            FinishProgramRpc();
            return;
        }
    }

    public IEnumerator InitiateBreakTime()
    {
        IsBreakTime = true;
        BallManager.instance.SetBallPositionToIdleRpc();

        while (exerciseProgress.Value.breakTimeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            exerciseProgress.Value = exerciseProgress.Value.DecrementBreakTime(1);
        }

        FinishBreak();
    }

    public void FinishBreak()
    {
        IsBreakTime = false;
        BallManager.instance.SetupBallRpc();
    }

    public bool IsSetCountReached()
    {
        return exerciseProgress.Value.setsDone == Exercises[currentExerciseIndex].Sets;
    }

    public bool IsRepCountReached()
    {
        return exerciseProgress.Value.repsDone == Exercises[currentExerciseIndex].Reps;
    }

    //called on owner side
    public void SetupNextExercise()
    {
        //link with canvas
        exerciseProgress.Value = new ExerciseProgress(currentExerciseIndex);
        Debug.Log($"The Exercise's Details: {Exercises[currentExerciseIndex]}");

        //set pose to match anchor point with
        Debug.Log($"Current Exercise Index: {currentExerciseIndex}");
        PlayExerciseAnimationRpc(false, currentExerciseIndex, PoseStates.End);

        SetupStartExerciseButtonRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void FinishProgramRpc()
    {
        //unsubscribe from events
        PoseConfirmationButton.action.started -= SetupAnchorPoint;
        exerciseProgress.OnValueChanged -= UpdateProgressUI;

        //despawn canvas
        Destroy(ProgressCanvasInstance);
    }


    [Rpc(SendTo.Server)]
    public void SetupStartExerciseButtonRpc()
    {
        StartExerciseButton.action.started += StartExercise;
    }

    [Rpc(SendTo.Server)]
    public void SetGameToIdleRpc()
    {
        BallManager.instance.SetBallPositionToIdleRpc();
        PlayerManager.instance.ClearBallHomePositions();
    }

    public void StartExercise(InputAction.CallbackContext context)
    {
        StartExerciseRpc();
    }

    [Rpc(SendTo.Server)]
    public void StartExerciseRpc()
    {
        if (!PlayerManager.instance.IsAllBallHomePositionsSet()) return;

        BallManager.instance.SetupBallRpc();
        PlayExerciseAnimationRpc(true);
        StartExerciseButton.action.started -= StartExercise;
    }

    [Rpc(SendTo.Server)]
    public void DebugRpc(string message, int value)
    {
        Debug.Log($"Message: {message}\nData: {value}");
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void PlayExerciseAnimationRpc(bool IsStartNow, int exerciseIndex = -1, PoseStates poseState = PoseStates.Start)
    {
        Debug.Log("Setup Pose");
        if (exerciseIndex == -1)
        {
            exerciseIndex = exerciseProgress.Value.exerciseIndex;
        }

        PoseManager.instance.SetupPose(Exercises[exerciseIndex].Name, IsStartNow);

        if (poseState != PoseStates.Start) PoseManager.instance.SetAvatarPose(poseState);

        Debug.Log("Player end Pose");
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

        foreach (TMP_Text tmp in exerciseStatsFields)
        {
            string gameObjectName = tmp.gameObject.name;

            if (gameObjectName == "Reps")
            {
                FieldNameReferencePair.Add("Reps", tmp);
            }
            else if (gameObjectName == "Sets")
            {
                FieldNameReferencePair.Add("Sets", tmp);
            }
            else if (gameObjectName == "ExerciseName")
            {
                FieldNameReferencePair.Add("ExerciseName", tmp);
            }
            else if (gameObjectName == "BreakTime")
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
        for (int i = 0; i < Exercises.Count; i++)
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
        for (int i = 0; i < exerciseConfigurations.Length; i++)
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

        public ExerciseProgress AddRep()
        {
            ExerciseProgress copy = this;
            copy.repsDone++;
            return copy;
        }

        public ExerciseProgress AddSet()
        {
            ExerciseProgress copy = this;
            copy.repsDone = 0;
            copy.setsDone++;
            copy.breakTimeLeft = 0;
            return copy;
        }

        public ExerciseProgress AddBreak()
        {
            ExerciseProgress copy = this;
            copy.breakTimeLeft = RehabProgram.Instance.Exercises[exerciseIndex].breakTimeSeconds;
            return copy;
        }

        public ExerciseProgress DecrementBreakTime(int value)
        {
            ExerciseProgress copy = this;
            copy.breakTimeLeft -= value;
            return copy;
        }

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