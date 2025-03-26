using System.Collections;
using TreeEditor;
using Unity.XR.CoreUtils;
using UnityEngine;

public class MoveTowardsPlayer : MonoBehaviour
{
    public Transform player1;
    public Transform player2;

    public bool followPlayer = false;

    public float deltaDistance = 0.1f;
    private Vector3 hitDirection = Vector3.zero;
    private Vector3 previousPosition = Vector3.zero;

    //bezier properties
    public float curveDuration = 1f;

    private IEnumerator coroutine;
    private bool isCoroutineRunning = false;

    // Update is called once per frame
    void Update()
    {
        previousPosition = transform.position;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!followPlayer) return;

        if(isCoroutineRunning) StopCoroutine(coroutine);

        Vector3 inNormal = collision.contacts[0].normal;
        Vector3 contactPoint = collision.contacts[0].point;
        Vector3 inDirection = transform.position - previousPosition;
        Vector3 outDirection = Vector3.Reflect(inDirection, inNormal);

        //debug vectors
        Debug.DrawRay(contactPoint, inDirection * 2f, Color.gray, 2f);
        Debug.DrawRay(contactPoint, inNormal, Color.red, 2f);
        Debug.DrawRay(contactPoint, outDirection * 10f, Color.green, 2f);

        hitDirection = outDirection;

        if (collision.gameObject.CompareTag("Racket"))
        {
            Debug.Log("Switch Turns to: " + collision.gameObject.name);
            PlayerManager.instance.SwitchTurn();
        }

        Vector3 p0;
        Vector3 p1;
        Vector3 p2;

        p0 = collision.contacts[0].point;
        p1 = p0 + hitDirection.normalized * 5f;
        p2 = PlayerManager.instance.GetCurrentPlayer().GetNamedChild("BallAnchor").transform.position;

        //the bezier curve points visualization;
        Debug.DrawLine(p0, p1, Color.blue, 2f);
        Debug.DrawLine(p1, p2, Color.blue, 2f);
        coroutine = FollowBezierCurveToPlayer(p0, p1, p2);

        StartCoroutine(coroutine);
        isCoroutineRunning = true;
    }

    private IEnumerator FollowBezierCurveToPlayer(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / curveDuration;
            transform.position = Mathf.Pow(1 - t, 2) * p0 +
                                 2 * (1 - t) * t * p1 +
                                 Mathf.Pow(t, 2) * p2;
            yield return null;
        }

        isCoroutineRunning = false;
    }
}
