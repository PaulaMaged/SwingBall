using System.Collections;
using UnityEngine;

public class BallPath : MonoBehaviour
{
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
        if (!followPlayer || !GameManagerTest.Instance.GameStarted) return;

        if (isCoroutineRunning) StopCoroutine(coroutine);

        Vector3 inNormal = collision.contacts[0].normal;
        Vector3 contactPoint = collision.contacts[0].point;

        Vector3 inDirection = transform.position - previousPosition;

        Vector3 outDirection;

        if (collision.gameObject.CompareTag("Racket"))
        {

            PlayerManagerTest.instance.SwitchTurn();
            //inDirection = TuneInDirection(inDirection);
        }

        if (inDirection == Vector3.zero)
        {
            outDirection = collision.contacts[0].normal;
        }
        else
        {
            outDirection = Vector3.Reflect(inDirection, inNormal);
            outDirection = TuneInDirection(outDirection);
        }

        //debug vectors
        Debug.DrawRay(contactPoint, inDirection * 5f, Color.magenta, 2f);
        Debug.DrawRay(contactPoint, inNormal * 5f, Color.red, 2f);
        Debug.DrawRay(contactPoint, outDirection * 5f, Color.green, 2f);

        hitDirection = outDirection;

        Vector3 p0;
        Vector3 p1;
        Vector3 p2;

        p0 = collision.contacts[0].point;
        p1 = p0 + hitDirection.normalized * 5f;
        p2 = PlayerManagerTest.instance.GetCurrentPlayerHomePosition();

        //the bezier curve points visualization;
        Debug.DrawLine(p0, p1, Color.blue, 2f);
        Debug.DrawLine(p1, p2, Color.blue, 2f);
        coroutine = FollowBezierCurveToPlayer(p0, p1, p2);

        StartCoroutine(coroutine);
        isCoroutineRunning = true;
    }

    private Vector3 TuneInDirection(Vector3 inDirection)
    {
        //get forward vector from player to pole
        Vector3 forward = BallManagerTest.instance.poleTransform.position - transform.position;
        if (Vector3.Dot(forward, inDirection) < 0)
        {
            Debug.Log("Flipped inDirection");
            return -inDirection;
        }

        return inDirection;
    }

    private IEnumerator FollowBezierCurveToPlayer(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float t = 0f;

        while (t <= 1f)
        {
            if (!followPlayer) yield return null;

            t += Time.deltaTime / curveDuration;
            transform.position = Mathf.Pow(1 - t, 2) * p0 +
                                 2 * (1 - t) * t * p1 +
                                 Mathf.Pow(t, 2) * p2;

            yield return null;
        }

        transform.position = p2;
        isCoroutineRunning = false;
    }
}
