using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;
using System;

public class RehabProgram : NetworkBehaviour
{
    public static RehabProgram Instance;
    public List<Exercise> Exercises;
    public GameObject ExerciseEntryPrefab;
    public Transform contentParent;
    public GameObject ProgressCanvasPrefab;
    private GameObject ProgressCanvasInstance;
    private Dictionary<string, TMP_Text> FieldNameReferencePair = new();
    private NetworkVariable<ExerciseProgress> exerciseProgress = new(writePerm: NetworkVariableWritePermission.Owner);

    private int currentExerciseIndex = 0;

    public void Awake()
    {
        Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsHost) return;

        ConfigureExercises();
    }

    private void ConfigureExercises()
    {
        foreach (Exercise exercise in Exercises)
        {
            GameObject entry = Instantiate(ExerciseEntryPrefab, contentParent);

            //replace with exercise name
            entry.transform.Find("Exercise Name").GetComponent<TMP_Text>().text = exercise.Name;

            //attach listener on value change to update exercise value
            GameObject SetsGO = entry.transform.Find("Sets").gameObject;
            TMP_Text setsUIText = SetsGO.transform.Find("value").GetComponent<TMP_Text>();

            Slider setsSlider = SetsGO.transform.Find("Slider").GetComponent<Slider>();
            setsSlider.onValueChanged.AddListener((value) =>
            {
                exercise.Sets = (int)value;
                setsUIText.text = value.ToString();
            });

            GameObject RepsGO = entry.transform.Find("Reps").gameObject;
            TMP_Text repsUIText = RepsGO.transform.Find("value").GetComponent<TMP_Text>();

            Slider repsSlider = RepsGO.transform.Find("Slider").GetComponent<Slider>();
            repsSlider.onValueChanged.AddListener((value) =>
            {
                exercise.Reps = (int)value;
                repsUIText.text = value.ToString();

            });

            GameObject BreakTimeGO = entry.transform.Find("BreakTime").gameObject;
            TMP_Text breakTimeUIText = BreakTimeGO.transform.Find("value").GetComponent<TMP_Text>();

            Slider BreakTimeSlider = BreakTimeGO.transform.Find("Slider").GetComponent<Slider>();
            BreakTimeSlider.onValueChanged.AddListener((value) =>
            {
                exercise.breakTimeSeconds = (int)value;
                breakTimeUIText.text = value.ToString();
            });
        }
    }

    public bool IsProgramCompleted() {
        if (currentExerciseIndex >= Exercises.Count)
            return true;
        else
            return false;
    }

    //manage exercises
    private void ExecuteExercises()
    {
        //check if exercise is completed
        //if(IsExerciseCompleted())
        //{
        //    currentExerciseIndex++;
        //    if(IsProgramCompleted())
        //    {
        //        //HandleProgramFinished();
        //        Debug.Log("Finished Program");
        //        return;
        //    } else
        //    {
        //        exerciseProgress.SetupExercise(currentExerciseIndex);
        //    }
        //}

        //check if currently taking break
        //if (IsBreakTime())
        //{
        //    UpdateBreakTime();
        //    return;
        //}

        //
        throw new NotImplementedException();
    }

    public bool IsExerciseCompleted()
    {
        //bool RepsCompleted = Exercises[currentExerciseIndex].Reps == exerciseProgress.repsDone;
        //bool SetsCompleted = Exercises[currentExerciseIndex].Sets == exerciseProgress.setsDone;

        //if(RepsCompleted && SetsCompleted)
        //    return true;
        //else
        //    return false;

        throw new NotImplementedException();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitiateExercisesRpc()
    {
        //setup canvas for stats, accuracy & character motion preview
        Vector3 canvasPosition = BallManager.instance.poleTransform.position + Vector3.forward * 10 + Vector3.up * 4;
        Quaternion canvasRotation = Quaternion.identity;
        ProgressCanvasInstance = Instantiate(ProgressCanvasPrefab, canvasPosition, canvasRotation);
        CacheUIFields();
        UpdateProgressUI();
        exerciseProgress.OnValueChanged += UpdateProgressUI;

        //Start Setting BallPositions through serverRPC as ball is owned by server

    }

    public void CacheUIFields()
    {
        //add TMP to dictionary
        TMP_Text[] exerciseStatsFields = ProgressCanvasInstance.transform.Find("Values").GetComponentsInChildren<TMP_Text>();
        
        foreach(TMP_Text tmp in exerciseStatsFields)
        {
            string gameObjectName = tmp.gameObject.name;
            Debug.Log($"GameObject Name: {gameObjectName}");

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
        Debug.Log("UI Update Trigger");
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

    public void UpdateProgressUI()
    {
        Debug.Log("UI Update Trigger");
        TMP_Text tmp;

        FieldNameReferencePair.TryGetValue("Reps", out tmp);
        tmp.text = exerciseProgress.Value.repsDone.ToString();

        FieldNameReferencePair.TryGetValue("Sets", out tmp);
        tmp.text = exerciseProgress.Value.setsDone.ToString();

        FieldNameReferencePair.TryGetValue("ExerciseName", out tmp);
        tmp.text = Exercises[exerciseProgress.Value.exerciseIndex].name;

        FieldNameReferencePair.TryGetValue("BreakTime", out tmp);
        tmp.text = exerciseProgress.Value.breakTimeLeft.ToString();
    }

    public void SetupNextExercise()
    {
        currentExerciseIndex++;

        exerciseProgress.Value = new ExerciseProgress(currentExerciseIndex);

        //setup home positions for players -- pressing button whilst inside of 

        //link with canvas

        //indicator for what the current static pose required is and when it is matched
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
}