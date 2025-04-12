using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

// Define a custom layout for your joystick
[InputControlLayout(displayName = "Custom Microntek Joystick")]
public class CustomMicrontekJoystick : Joystick
{
    // Instead of using the nested Dpad, we define a flat vector for directional input.
    // You can adjust these as needed to suit the physical device.

    [InputControl(name = "dpad", layout = "Vector2", usage = "Dpad", offset = 0)]
    public Vector2Control dpad { get; private set; }

    protected override void FinishSetup()
    {
        // Complete the setup by grabbing the dpad control from the device
        dpad = GetChildControl<Vector2Control>("dpad");
        base.FinishSetup();
    }
}
