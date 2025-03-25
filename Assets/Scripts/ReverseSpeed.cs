using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.OpenXR.Input;

public class ReverseSpeed : MonoBehaviour
{
    public Transform poleTransform;

    [SerializeField]
    private float ropeLength;

    private Rigidbody rigidBody;
    private Vector3 relativePosition = new(-3, 0, 0);
    private RotatePivot rotatePivotScript;

    private Vector3 offset;

    void Start()
    {
        rotatePivotScript = GetComponentInParent<RotatePivot>();
        rigidBody = GetComponent<Rigidbody>();
        offset = transform.position - poleTransform.position;
    }

    private void FixedUpdate()
    {
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.linearVelocity = Vector3.zero;
    }

    public void OnCollisionEnter(Collision other)
    {
        Debug.Log("Change direction collision");
        Vector3 contactNormal = other.contacts[0].normal;
        Vector3 bounceDirection = Vector3.Reflect(offset, contactNormal);
        bounceDirection.Normalize();

        rotatePivotScript.ChangeDirection(bounceDirection);

    }
}
