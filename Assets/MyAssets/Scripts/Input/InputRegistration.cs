using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public static class InputRegistration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterCustomLayout()
    {
        InputSystem.RegisterLayout<CustomMicrontekJoystick>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Microntek\\s*")
                .WithProduct("USB Joystick\\s*")
        );

        foreach (var device in InputSystem.devices)
        {
            if (device.layout == "Joystick") // Only replace generic ones
            {
                InputSystem.RemoveDevice(device);
                InputSystem.AddDevice("CustomJoystick");
            }
        }
    }
}
