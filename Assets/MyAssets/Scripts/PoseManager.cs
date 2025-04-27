using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PoseManager : NetworkBehaviour
{
    public static PoseManager instance;
    private Animator animator;

    public string ExerciseName { get; private set; } //must be exact as state names is derived from it to play animation
    private PoseStates currentPoseState = PoseStates.None;

    public bool IsPoseCycleComplete { get; private set; } = false;

    [SerializeField] int maxRandomValueStart = 10;
    [SerializeField] int maxRandomValueEnd = 10;

    void Awake()
    {
        instance = this;
    }

    public bool HasSatisfiedPoseAccuracy()
    {

        return FakeBool();
    }

    private bool FakeBool()
    {
        int randomInt = UnityEngine.Random.Range(0, currentPoseState == PoseStates.Start ? maxRandomValueStart : maxRandomValueEnd);

        Debug.Log($"Random Value Is: {randomInt}");
        if (randomInt == 0) return true;
        return false;
    }

    public bool HasCompletedMotion()
    {
        if (IsPoseCycleComplete && HasSatisfiedPoseAccuracy()) return true;
        Debug.Log("Motion Isn't Completed");
        return false;
    }

    public void SetAnimator(Animator animator)
    {
        this.animator = animator;
    }

    public void NextPose()
    {
        if (currentPoseState != PoseStates.Start && currentPoseState != PoseStates.End)
        {
            throw new Exception("Pose states at this point in time should only be this or that");
        }

        if (currentPoseState == PoseStates.Start)
        {
            Debug.Log("Start Pose Satisfied");
            IsPoseCycleComplete = true;
            currentPoseState = PoseStates.End;
        }
        else if (currentPoseState == PoseStates.End)
        {
            Debug.Log("End Pose Satisfied");
            IsPoseCycleComplete = false;
            currentPoseState = PoseStates.Start;
            StartCoroutine(ListenToPoseCompletion());
        }

        SetAvatarPose(currentPoseState);
    }

    private IEnumerator ListenToPoseCompletion()
    {
        while (!HasSatisfiedPoseAccuracy())
        {
            yield return null;
        }

        IsPoseCycleComplete = true;
        NextPose();
    }

    internal void SetupPose(string exerciseName, bool IsStartNow = true)
    {
        if (animator == null)
        {
            throw new NullReferenceException("Can't Setup Pose without having setup an animator");
        }

        ExerciseName = exerciseName;
        SetAvatarPose(PoseStates.Start);

        StopAllCoroutines();
        IsPoseCycleComplete = false;

        if (IsStartNow) StartCoroutine(ListenToPoseCompletion());

    }

    public void SetAvatarPose(PoseStates poseState)
    {
        currentPoseState = poseState;
        string stateName = GetPoseName(poseState);
        PlayAnimation(stateName);
    }

    public string GetPoseName(PoseStates poseState)
    {
        switch (poseState)
        {
            case PoseStates.None: return "Empty";
            case PoseStates.Start: return ExerciseName + "Start";
            case PoseStates.Continuous: throw new NotImplementedException("This state has not been implemented yet");
            case PoseStates.End: return ExerciseName + "End";
            default: throw new Exception("Impossible!");
        }
    }

    public void PlayAnimation(string stateName)
    {
        animator.Play(stateName);
    }
}

public enum PoseStates
{
    None,
    Start,
    Continuous,
    End,
}
