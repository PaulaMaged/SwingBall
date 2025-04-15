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
}
