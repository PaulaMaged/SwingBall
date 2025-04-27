using UnityEngine;

public class ReverseSpeed : MonoBehaviour
{
    public Transform poleTransform;

    [SerializeField]
    private float ropeLength;

    private Rigidbody rigidBody;
    private Vector3 relativePosition = new(-3, 0, 0);
    private RotatePivot rotatePivotScript;

    private Vector3 previousDirection = Vector3.zero;
    private Vector3 offset;

    void Start()
    {
        rotatePivotScript = GetComponentInParent<RotatePivot>();
        rigidBody = GetComponent<Rigidbody>();
        offset = (transform.position - poleTransform.position).normalized;
    }

    private void Update()
    {
        previousDirection = transform.position - previousDirection;
    }
    public void OnCollisionEnter(Collision other)
    {
        Debug.Log("Change direction collision");
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 contactNormal = other.contacts[0].normal;
        Vector3 inDirection = transform.position - previousDirection;
        inDirection.Normalize();
        Vector3 reflectedDirection = Vector3.Reflect(inDirection, contactNormal).normalized;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(reflectedDirection, offset).normalized;
        
        Debug.DrawRay(contactPoint, inDirection * 2f, Color.gray, 2f);
        Debug.DrawRay(contactPoint, contactNormal, Color.red, 2f);
        Debug.DrawRay(contactPoint, reflectedDirection * 10f, Color.green, 2f);
        Debug.DrawRay(contactPoint, projectedDirection * 5f, Color.blue, 2f);

        rotatePivotScript.ChangeDirection(reflectedDirection);
    }
}
