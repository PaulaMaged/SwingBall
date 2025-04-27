using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

public class ExerciseSync : NetworkBehaviour
{
    public Exercise Exercise;
    public NetworkVariable<ExerciseData> Data;
    public Dictionary<string, Slider> FieldNameToSlider;

    public ExerciseSync(Exercise exercise, GameObject exerciseEntryPrefab, Transform contentParent)
    {
        Exercise = exercise;

        Data = new(writePerm: NetworkVariableWritePermission.Server)
        {
            Value = new(exercise)
        };
    }
}

