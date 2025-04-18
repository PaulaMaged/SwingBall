using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SetupPeerType : MonoBehaviour
{
    // Timeout in seconds to wait for a successful client connection.
    [SerializeField] private float connectionTimeout = 5.0f;

    private void Start()
    {
        StartCoroutine(AttemptToStartClientOrHost());
    }

    private IEnumerator AttemptToStartClientOrHost()
    {
        // Attempt to start as a client first.
        NetworkManager.Singleton.StartClient();
        Debug.Log("Attempting to connect as Client...");

        // Wait for a certain timeout period.
        float timer = 0f;
        bool connected = false;
        while (timer < connectionTimeout)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                connected = true;
                Debug.Log("Successfully connected as Client.");
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // If not connected after the timeout, assume there’s no host.
        if (!connected)
        {
            // Shutdown the unsuccessful client attempt.
            NetworkManager.Singleton.Shutdown();
            yield return null; // Optionally wait one frame before restarting.
            Debug.Log("No host detected. Starting as Host...");
            NetworkManager.Singleton.StartHost();
        }
    }
}
