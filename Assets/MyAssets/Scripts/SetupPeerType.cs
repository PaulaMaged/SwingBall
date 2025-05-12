using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SetupPeerType : MonoBehaviour
{
    // Timeout in seconds to wait for a successful client connection.
    [Tooltip("Server Address")]
    [SerializeField] private string _serverIPAddress = "192.168.1.1";
    [SerializeField] private ushort _portNumber = 7777;

    [SerializeField] private float connectionTimeout = 5.0f;

    private void Start()
    {
        StartCoroutine(AttemptToStartClientOrHost());
    }

    private IEnumerator AttemptToStartClientOrHost()
    {
        // Attempt to start as a client first.
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            _serverIPAddress,
            _portNumber,
            "0.0.0.0"
            );

        NetworkManager.Singleton.StartClient();
        Debug.Log("Attempting to connect as Client...");

        // Wait for a certain timeout period.
        float timer = 0f;
        bool connected = false;
        while (timer < connectionTimeout)
        {

            bool? hasFoundConnection = NetworkManager.Singleton == null ? null : NetworkManager.Singleton.IsConnectedClient;
            if (hasFoundConnection == null) continue;

            if(hasFoundConnection.Value) {
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
