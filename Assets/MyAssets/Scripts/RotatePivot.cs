using UnityEngine;

public class RotatePivot : MonoBehaviour
{
    [SerializeField]
    private Vector3 rotationDirection;

    private GameObject ball;

    public float rotationSpeed = 1;

    private Vector3 offset;
    private Vector3 rotationAxis;

    public void Start()
    {
        ball = GameObject.Find("MyTennisBall");
        offset = ball.transform.position - transform.position; // Initial offset from 
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion deltaRotation = Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, rotationAxis);
        transform.rotation *= deltaRotation;
    }
    public void ChangeDirection(Vector3 newDirection)
    {
        rotationDirection = newDirection;
        rotationAxis = Vector3.Cross(offset, rotationDirection).normalized;
        Debug.DrawRay(transform.position, rotationAxis, Color.yellow, 2f);
    }
}
