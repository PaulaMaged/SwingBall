using UnityEngine;
using UnityEngine.InputSystem;

public class ForceRegisterDevice : MonoBehaviour
{
    void Start()
    {
        // This attempts to add a new device using your custom layout.
        var device = InputSystem.AddDevice<CustomMicrontekJoystick>();
        Debug.Log("Device added: " + device);
    }
}