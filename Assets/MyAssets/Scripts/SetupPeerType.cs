using com.rfilkov.kinect;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEditor.Experimental.GraphView;
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

    private readonly object responseLock = new();
    private bool bBroadcastResponseReceived;
    private UdpClient udpClient = null;
    private UdpBroadcastServer autoDiscoveryServer;
    private bool finishedStartup = false;

    private void Start()
    {
        startTime = Time.time;
        Task.Run(() => BroadcastServerDiscovery());

        if (NetworkManager.Singleton != null)
            LogDisconnectionMessages(NetworkManager.Singleton);
        else
            NetworkManager.OnInstantiated += (NetworkManager netManager) => LogDisconnectionMessages(netManager);

        }
    private void LogDisconnectionMessages(NetworkManager netManager)
    {
        netManager.OnClientDisconnectCallback += (ulong clientId) =>
        {
            var transport = (UnityTransport)netManager.NetworkConfig.NetworkTransport;

            if (!netManager.IsServer)
            {
                NetworkEndpoint ep = transport.GetLocalEndpoint();
                if(ep.IsValid) 
                    Debug.LogError($"Disconnected from server. Local IP details:\nIP: {ep.Address} \nPort: {ep.Port}");
            }
            else
            {
                NetworkEndpoint ep = transport.GetEndpoint(clientId);
                if(ep.IsValid)
                    Debug.LogError($"Client disconnected from the server \nIP: {ep.Address} \nPort: {ep.Port}");
            }

        };
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
                autoDiscoveryServer = new UdpBroadcastServer(UDPDiscoveryPort, "DiscoveryServer", "NET", responseData, new System.Text.StringBuilder());

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                    _serverIPAddress,
                    _portNumber,
                    "0.0.0.0"
                    );

                Debug.Log("Auto discovery - server host: " + _serverIPAddress + ", port: " + _portNumber);

                NetworkManager.Singleton.StartHost();
                finishedStartup = true;

                lock (responseLock)
                {
                    bBroadcastResponseReceived = true; //just to avoid more incoming responses.
                    udpClient?.Close();
                }
            }
        }
        catch (SocketException e)
        {
            Debug.LogError($"Socket Exception: Error Code ({e.SocketErrorCode}) \n {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void BroadcastServerDiscovery()
    {
        Debug.Log("Searching for servers to connect to");

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
                    udpClient.Send(btRequestData, btRequestData.Length, new IPEndPoint(IPAddress.Broadcast, UDPDiscoveryPort));
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error occured while finding server ip: {ex}");
        }
    }

    // invoked when a response from the broadcast server gets received
    private void BroadcastServerResponseReceived(System.IAsyncResult ar)
    {
        try
        {
            lock (responseLock)
            {
                if (bBroadcastResponseReceived)
                {
                    Debug.Log("Disregarding response from broadcast Server");
                    return;
                }

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
                        uc?.Close();
                        Debug.Log("Disposed udpClient");
                    }
                }
                if (!bBroadcastResponseReceived)
                {
                    uc.BeginReceive(new System.AsyncCallback(BroadcastServerResponseReceived), state);
                }
            }
        }
        catch (System.Net.Sockets.SocketException sockEx)
        {
            Debug.LogError($"error receiving response: {sockEx.Message} (SocketErrorCode: {sockEx.SocketErrorCode} {sockEx.StackTrace})");
        }
        catch (System.IO.IOException ioEx)
        {
            Debug.LogError($"IO errorReceving Response: {ioEx.Message} {ioEx.StackTrace}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"unexpected error sending to client: {ex.GetType().Name}: {ex.Message} {ex.StackTrace}");
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
        if (NetworkManager.Singleton) NetworkManager.Singleton.Shutdown();
        autoDiscoveryServer?.Close();
    }
}