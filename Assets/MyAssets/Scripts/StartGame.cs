using UnityEngine;

public class StartGame : MonoBehaviour
{
    public void StartTheGame()
    {
        GameManager.Instance.StartGame();
    }
}
