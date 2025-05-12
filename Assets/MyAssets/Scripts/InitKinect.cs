using com.rfilkov.kinect;
using UnityEngine;

public class InitKinect : MonoBehaviour
{
    public KinectNetServer KinectNetServer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        KinectManager.Instance.StartDepthSensors();
        if(KinectNetServer != null) KinectNetServer.StartServer();
    }
}
