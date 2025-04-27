using UnityEngine;

public class AnimateRig : MonoBehaviour
{
    public Animator animator;
    public XRRigReferences _XRRigReferences;

    private Transform Head;
    private Transform LeftHand;
    private Transform RightHand;

    public GameObject RBat;
    public GameObject LBat;

    public Vector3 RBatPositionOffset;
    public Vector3 RBatRotationOffset;

    public Vector3 LBatPositionOffset;
    public Vector3 LBatRotationOffset;

    public Vector3 CameraPositionOffset;
    public Vector3 CameraRotationOffset;

    public Vector3 LeftControllerPositionOffset;
    public Vector3 LeftControllerRotationOffset;

    public Vector3 RightControllerPositionOffset;
    public Vector3 RightControllerRotationOffset;

    private void Start()
    {
        Head = animator.GetBoneTransform(HumanBodyBones.Head);
        LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }
    void Update()
    {
        SetPositionWOffset(_XRRigReferences.Camera, Head, CameraPositionOffset);
        SetRotationWOffset(Head, _XRRigReferences.Camera, CameraRotationOffset);

        SetPositionWOffset(_XRRigReferences.LeftController, LeftHand, LeftControllerPositionOffset);
        SetRotationWOffset(LeftHand, _XRRigReferences.LeftController, LeftControllerRotationOffset);

        SetPositionWOffset(_XRRigReferences.RightController, RightHand, RightControllerPositionOffset);
        SetRotationWOffset(RightHand, _XRRigReferences.RightController, RightControllerRotationOffset);

        SetPositionWOffset(RBat.transform, RightHand, RBatPositionOffset);
        SetRotationWOffset(RBat.transform, _XRRigReferences.RightController, RBatRotationOffset);

        SetPositionWOffset(LBat.transform, LeftHand, LBatPositionOffset);
        SetRotationWOffset(LBat.transform, _XRRigReferences.LeftController, LBatRotationOffset);
    }
    private void SetPositionWOffset(Transform copyTo, Transform copyFrom, Vector3 positionOffset)
    {
        copyTo.position = copyFrom.position + copyFrom.rotation * positionOffset;
    }
    private void SetRotationWOffset(Transform copyTo, Transform copyFrom, Vector3 rotationOffset)
    {
        copyTo.rotation = copyFrom.rotation * Quaternion.Euler(rotationOffset);
    }
}
