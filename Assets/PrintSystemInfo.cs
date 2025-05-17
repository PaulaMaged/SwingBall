using UnityEngine;

public class PrintSystemInfo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"{SystemInfo.graphicsDeviceName}");
        Debug.Log($"{SystemInfo.graphicsDeviceVendor}");
    }
}
