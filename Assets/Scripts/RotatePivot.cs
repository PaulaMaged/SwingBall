using UnityEngine;

public class RotatePivot : MonoBehaviour
{
    public Vector3 rotationVector = Vector3.up;
    public float rotationSpeed = 1;
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationVector * rotationSpeed * Time.deltaTime);
    }
}
