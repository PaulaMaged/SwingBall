using Unity.Netcode;
using UnityEngine;

public class BallManager : NetworkBehaviour
{
    public static BallManager instance;

    public GameObject ball;
    private Vector3 BallPositionInitial;
    public Transform poleTransform;

    private MoveTowardsPlayer ballMovementScript;
    private Vector3 startPosition;
    

    private void Awake()
    {
        if (instance == null) instance = this;
        ballMovementScript = ball.GetComponent<MoveTowardsPlayer>();
        BallPositionInitial = ball.transform.position;
    }

    [Rpc(SendTo.Server)]
    public void SetupBallRpc()
    {
        Vector3 ballHomePosition = PlayerManager.instance.GetCurrentPlayerBallHomePosition();

        startPosition = ballHomePosition + (Vector3.right * poleTransform.position.x - ballHomePosition).normalized * 0.1f;

        ball.transform.position = startPosition;
        ballMovementScript.followPlayer = true;
    }

    [Rpc(SendTo.Server)]
    public void SetBallPositionToIdleRpc()
    {
        Debug.Log(BallPositionInitial);
        ballMovementScript.StopMovement(BallPositionInitial);
    }
}
