using com.rfilkov.kinect;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SetupPeerType : MonoBehaviour
{
    // Timeout in seconds to wait for a successful client connection.
    [Tooltip("Server Address")]
    [SerializeField] private string _serverIPAddress = "127.0.0.1";
    [SerializeField] private ushort _portNumber = 7777;
    [SerializeField] private ushort UDPDiscoveryPort = 9000;

    private float startTime;
    [SerializeField] private float connectionTimeout = 10.0f;

    private bool bBroadcastResponseReceived;
    private UdpBroadcastServer autoDiscoveryServer;
    private bool finishedStartup = false;

    private void Start()
    {
        startTime = Time.time;
        Task.Run(() => BroadcastServerDiscovery());
    }

    private void Update()
    {
        if (finishedStartup) return;
        try
        {
            if (bBroadcastResponseReceived)
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                    _serverIPAddress,
                    _portNumber,
                    "0.0.0.0"
                    );

                Debug.Log("Auto discovery - server host: " + _serverIPAddress + ", port: " + _portNumber);

                NetworkManager.Singleton.StartClient();
                finishedStartup = true;
            }
            else if ((Time.time - startTime) > connectionTimeout)
            {
                Debug.Log("No host detected. Starting as Host...");

                _serverIPAddress = GetLocalNameOrIP();
                string responseData = string.Format("NET|{0}|{1}", _serverIPAddress, _portNumber);
                autoDiscoveryServer = new UdpBroadcastServer(UDPDiscoveryPort, "DiscoveryServer", "NET", responseData, null);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                    _serverIPAddress,
                    _portNumber,
                    "0.0.0.0"
                    );

                Debug.Log("Auto discovery - server host: " + _serverIPAddress + ", port: " + _portNumber);

                NetworkManager.Singleton.StartHost();
                finishedStartup = true;
            }
        } catch (SocketException e)
        {
            Debug.LogError($"Socket Exception: Error Code ({e.SocketErrorCode}) \n {e.Message}");
        } catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void BroadcastServerDiscovery()
    {
        Debug.Log("Searching for servers to connect to");

        UdpClient udpClient = null;
        //Debug.Log("Auto discovery - looking for net server...");

        udpClient = new UdpClient();
        IPEndPoint ep = new(IPAddress.Any, 0);

        string sRequestData = "NET";
        byte[] btRequestData = System.Text.Encoding.UTF8.GetBytes(sRequestData);

        udpClient.EnableBroadcast = true;

        // wait for response
        // wait for net-cst
        long timeStart = System.DateTime.Now.Ticks;
        long timeNow = timeStart;

        try
        {
            udpClient.Send(btRequestData, btRequestData.Length, new IPEndPoint(IPAddress.Broadcast, UDPDiscoveryPort));

            UdpState state = new UdpState();
            state.ep = ep;
            state.uc = udpClient;

            udpClient.BeginReceive(new System.AsyncCallback(BroadcastServerResponseReceived), state);

            while (!bBroadcastResponseReceived && (timeNow - timeStart) < connectionTimeout)
            {
                Thread.Sleep(100);
                timeNow = System.DateTime.Now.Ticks;

                if ((ulong)(timeNow - timeStart) > 10_000_000 * 1)
                {
                    timeStart = System.DateTime.Now.Ticks;
                    udpClient.Send(btRequestData, btRequestData.Length, new IPEndPoint(IPAddress.Broadcast, _portNumber));
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error occured while finding server ip: {ex}");
        }

        udpClient?.Close();
    }

    // invoked when a response from the broadcast server gets received
    private void BroadcastServerResponseReceived(System.IAsyncResult ar)
    {
        UdpState state = (UdpState)(ar.AsyncState);
        UdpClient uc = state.uc;
        IPEndPoint ep = state.ep;

        byte[] btReceiveData = uc.EndReceive(ar, ref ep);
        string sReceiveData = System.Text.Encoding.UTF8.GetString(btReceiveData);
        Debug.Log("Received response from the broadcast server.");

        if (!string.IsNullOrEmpty(sReceiveData))
        {
            string[] asParts = sReceiveData.Split("|".ToCharArray());

            if (asParts.Length >= 3 && asParts[0] == "NET")
            {
                _serverIPAddress = asParts[1];
                ushort.TryParse(asParts[2], out _portNumber);
                bBroadcastResponseReceived = true;
            }
        }

        if (!bBroadcastResponseReceived)
        {
            uc.BeginReceive(new System.AsyncCallback(BroadcastServerResponseReceived), state);
        }

    }

    private string GetLocalNameOrIP()
    {
        string localIP = "localhost";

        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    if (localIP == "0.0.0.0" || localIP.StartsWith("169.254."))
                        continue;

                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        return localIP;
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton?.Shutdown();
    }
}