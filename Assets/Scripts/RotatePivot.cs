using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class RotatePivot : MonoBehaviour
{
    [SerializeField]
    private Vector3 rotationDirection;

    private GameObject ball;

    public float rotationSpeed = 1;

    private Vector3 offset;

    public void Start()
    {
        ball = GameObject.Find("MyTennisBall");
        offset = ball.transform.position - transform.position; // Initial offset from 
    }

    // Update is called once per frame
    void Update()
    {
        //Vector3 radial = (ball.transform.position - transform.position).normalized;
        //Vector3 rotationAxis = Vector3.Cross(radial, rotationDirection).normalized;


        Vector3 rotationAxis = Vector3.Cross(offset, rotationDirection).normalized;
        transform.RotateAround(transform.position, rotationAxis, Time.deltaTime * rotationSpeed);
    }

    public void ChangeDirection(Vector3 newDirection)
    {
        rotationDirection = newDirection;
    }
}
