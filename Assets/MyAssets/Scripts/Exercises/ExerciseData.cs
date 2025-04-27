using Unity.Collections;
using Unity.Netcode;

public struct ExerciseData : INetworkSerializable
{
    public FixedString64Bytes Name; 
    public int Reps;
    public int Sets;
    public int breakTimeSeconds;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Reps);
        serializer.SerializeValue(ref Sets);
        serializer.SerializeValue(ref breakTimeSeconds);
    }

    public ExerciseData(Exercise exerciseSO)
    {
        Name = exerciseSO.Name;
        Reps = exerciseSO.Reps;
        Sets = exerciseSO.Sets;
        breakTimeSeconds = exerciseSO.breakTimeSeconds;
    }
}
