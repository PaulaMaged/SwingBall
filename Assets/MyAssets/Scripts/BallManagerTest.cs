using UnityEngine;

public class BallManagerTest : MonoBehaviour
{
    public static BallManagerTest instance;

    public GameObject ball;
    public Transform poleTransform;

    private Vector3 startPosition;

    private void Awake()
    {
        if (instance == null) instance = this;
    }
    public void SetupBall()
    {
        Vector3 ballHomePosition = PlayerManagerTest.instance.GetCurrentPlayerHomePosition();

        startPosition = ballHomePosition + (Vector3.right * poleTransform.position.x - ballHomePosition).normalized * 0.1f;

        ball.transform.position = startPosition;
    }
}
