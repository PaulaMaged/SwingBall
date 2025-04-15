using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;

public class BallManager : NetworkBehaviour
{
    public static BallManager instance;

    public GameObject ball;
    private Vector3 BallPositionInitial;
    private MoveTowardsPlayer ballMovementScript;
    public Transform poleTransform;

    private Vector3 startPosition;

    private void Awake()
    {
        if (instance == null) instance = this;
        ballMovementScript = ball.GetComponent<MoveTowardsPlayer>();
        BallPositionInitial = ball.transform.position;
    }

    public void SetupBall()
    {
        Vector3 ballHomePosition = PlayerManager.instance.GetCurrentPlayerBallHomePosition();

        startPosition = ballHomePosition + (Vector3.right * poleTransform.position.x - ballHomePosition).normalized * 0.1f;

        ball.transform.position = startPosition;
        ballMovementScript.followPlayer = true;
    }

    public void SetBallPositionToIdle()
    {
        ballMovementScript.followPlayer = false;
        ball.transform.position = BallPositionInitial;
    }
}
