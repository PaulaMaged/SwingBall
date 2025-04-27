using com.rfilkov.kinect;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewExercise", menuName = "Rehab/Exercise")]
public class Exercise : ScriptableObject
{
    public string Name;
    public AnimationClip Motion;
    public AnimationClip StartPose;
    public AnimationClip EndPose;
    public int Reps;
    public int Sets;
    public int breakTimeSeconds;

    [SerializeField] NewDict JointInfoList;

    private Dictionary<KinectInterop.JointType, JointInfo> _joint2WeightAndMaxAngle;
    public Dictionary<KinectInterop.JointType, JointInfo> Joint2WeightAndMaxAngle
    {
        get
        {
            _joint2WeightAndMaxAngle = JointInfoList.ToDictionary();
            return _joint2WeightAndMaxAngle;
        }
        private set
        {
            _joint2WeightAndMaxAngle = value;
        }
    }

    public override string ToString()
    {
        string stringRepresentation = "";

        stringRepresentation += "---";
        stringRepresentation += $"Name:\t{Name}\n";
        stringRepresentation += $"Reps:\t{Reps}\n";
        stringRepresentation += $"Sets:\t{Sets}\n";
        stringRepresentation += $"Reps:\t{breakTimeSeconds}";
        stringRepresentation += "---";

        return stringRepresentation;
    }

    [Serializable]
    class NewDict
    {
        public List<DictItem> items = new();

        public Dictionary<KinectInterop.JointType, JointInfo> ToDictionary()
        {
            Dictionary<KinectInterop.JointType, JointInfo> joint2Weight = new();
            foreach (var item in items)
            {
                JointInfo jointInfo = new(item.JointWeight, item.JointMaxAngle);

                joint2Weight.Add(item.JointType, jointInfo);
            }

            return joint2Weight;
        }
    }

    [Serializable]
    class DictItem
    {
        public KinectInterop.JointType JointType;

        [Range(0f, 1f)]
        public float JointWeight = 0f;

        [Range(90, 180)]
        public int JointMaxAngle = 90;
    }

    private void OnValidate()
    {
        CheckWeights(false);
    }

    private void CheckWeights(bool adjust)
    {
        float totalWeight = 0;
        foreach (DictItem item in JointInfoList.items)
        {
            totalWeight += item.JointWeight;
        }

        // If total weight exceeds 1, reset weights or display a warning
        if (totalWeight > 1f)
        {

            // Option 2: Alternatively, you could display a message or reset everything
            Debug.LogWarning("Total weight exceeds 1. Values have been adjusted.");

            if (!adjust) return;

            // Option 1: Clamp the weights to ensure the sum doesn't exceed 1
            float excess = totalWeight - 1f;
            for (int i = 0; i < JointInfoList.items.Count; i++)
            {
                JointInfoList.items[i].JointWeight -= excess * (JointInfoList.items[i].JointWeight / totalWeight);
            }
        }

    }
}

public readonly struct JointInfo
{
    public float Weight { get; }
    public int MaxAngle { get; }

    public JointInfo(float weight, int maxAngle)
    {
        this.Weight = weight;
        this.MaxAngle = maxAngle;
    }
}