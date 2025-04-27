using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MoveTowardsPlayer : NetworkBehaviour
{
    public bool followPlayer = false;

    private Vector3 hitDirection = Vector3.zero;
    private Vector3 previousPosition = Vector3.zero;

    //bezier properties
    public float curveDuration = 1f;

    private IEnumerator coroutine;
    private bool isCoroutineRunning = false;
    private BallCollisionInfo LastBallCollisionInfo = new();

    // Update is called once per frame
    void Update()
    {
        previousPosition = transform.position;
    }

    public void StopMovement(Vector3 position = default)
    {
        followPlayer = false;
        StopCoroutine();

        if (position != default)
        {
            transform.position = position;
            Debug.Log($"Ball at initial position {transform.position}");
        }
    }

    public void StopCoroutine()
    {
        if (isCoroutineRunning)
        {
            StopCoroutine(coroutine);
            isCoroutineRunning = false;
        }
    }

    private void SetBallPath(BallCollisionInfo ballCollisionInfo)
    {
        Collision collision = ballCollisionInfo.collision;
        Vector3 inDirection = ballCollisionInfo.BallInDirection;

        Vector3 inNormal = collision.contacts[0].normal;
        Vector3 contactPoint = collision.contacts[0].point;

        Vector3 outDirection;

        if (inDirection == Vector3.zero)
        {
            outDirection = collision.contacts[0].normal;
        }
        else
        {
            outDirection = Vector3.Reflect(inDirection, inNormal);
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
        p2 = PlayerManager.instance.GetCurrentPlayerBallHomePosition();

        //the bezier curve points visualization;
        Debug.DrawLine(p0, p1, Color.blue, 2f);
        Debug.DrawLine(p1, p2, Color.blue, 2f);
        coroutine = FollowBezierCurveToPlayer(p0, p1, p2);

        StartCoroutine(coroutine);
        isCoroutineRunning = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !followPlayer || !GameManager.Instance.GameStarted) return;

        StopCoroutine();

        LastBallCollisionInfo.Update(transform.position - previousPosition, collision);

        if (collision.gameObject.CompareTag("Racket"))
        {
            //only call the movement execution if the client is the one responsible for the hit(the patient)
            if (collision.transform.root.gameObject == PlayerManager.instance.Players[1]) RehabProgram.Instance.OnMovementExecutionRpc();
            PlayerManager.instance.SwitchTurnRpc();
        }

        SetBallPath(LastBallCollisionInfo);
    }

    private IEnumerator FollowBezierCurveToPlayer(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float t = 0f;

        while (t <= 1f)
        {
            if (!followPlayer) yield break;

            t += Time.deltaTime / curveDuration;
            transform.position = Mathf.Pow(1 - t, 2) * p0 +
                                 2 * (1 - t) * t * p1 +
                                 Mathf.Pow(t, 2) * p2;

            yield return null;
        }

        transform.position = p2;
        isCoroutineRunning = false;
    }

    private struct BallCollisionInfo
    {
        public Vector3 BallInDirection;
        public Collision collision;

        public void Update(Vector3 BallInDirection, Collision collision)
        {
            this.BallInDirection = BallInDirection;
            this.collision = collision;
        }
    }
}
