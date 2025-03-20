using UnityEngine;

public class ReverseSpeed : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private RotatePivot rotatePivotScript;
    void Start()
    {
        rotatePivotScript = GetComponentInParent<RotatePivot>();
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");
        if(other.gameObject.CompareTag("Racket"))
        {
            rotatePivotScript.rotationSpeed *= -1;
        }
    }
}
