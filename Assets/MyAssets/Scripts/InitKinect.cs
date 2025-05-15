using com.rfilkov.kinect;
using UnityEngine;

public class InitKinect : MonoBehaviour
{
    public KinectNetServer KinectNetServer;

    void Awake()
    {
        KinectManager.Instance.StartDepthSensors();
    }

    private void Start()
    {
        if (KinectNetServer != null) KinectNetServer.StartServer();
    }
}
