using UnityEngine;

public class ShowCollisionDebugInfo : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Vector3 startPoint = collision.contacts[0].point;
        Vector3 normalDirection = collision.contacts[0].normal;
        Debug.DrawRay(startPoint, normalDirection, Color.red, 2f);
    }

}
