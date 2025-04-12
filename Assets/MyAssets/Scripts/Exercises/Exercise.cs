using UnityEngine;

[CreateAssetMenu(fileName = "NewExercise", menuName = "Rehab/Exercise")]
public class Exercise : ScriptableObject
{
    public string Name;
    public Animation motion;
    public int Reps;
    public int Sets;
    public int breakTimeSeconds;
}
