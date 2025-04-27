using UnityEngine;

public class GameManagerTest : MonoBehaviour
{
    public static GameManagerTest Instance { get; private set; }
    public bool GameStarted { get; private set; } = false;

    [SerializeField] private GameObject hostCanvas;

    void Awake()
    {
        Instance = this;
    }
    public void StartGame()
    {
        PlayerManagerTest.instance.InitiateHomePositions();
        BallManagerTest.instance.SetupBall();
        hostCanvas.SetActive(false);
        GameStarted = true;
    }
}
