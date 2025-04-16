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
}
