using UnityEngine;

public class BallManager : MonoBehaviour
{
    public GameObject ball;
    public Transform poleTransform;

    private Vector3 startPosition;

    void Start()
    {
        GameObject currentPlayer = PlayerManager.instance.GetCurrentPlayer();

        startPosition = currentPlayer.transform.position + (Vector3.right * poleTransform.position.x - currentPlayer.transform.position).normalized;

        ball.transform.position = startPosition;
    }
}
