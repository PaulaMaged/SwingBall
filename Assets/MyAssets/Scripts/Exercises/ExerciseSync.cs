using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

