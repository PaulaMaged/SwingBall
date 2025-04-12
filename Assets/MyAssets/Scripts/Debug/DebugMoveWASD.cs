using UnityEngine;
using UnityEngine.InputSystem;

public class DebugMoveWASD : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f;

    void Update()
    {
        Vector3 translation = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) translation += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) translation += Vector3.back;
        if (Keyboard.current.aKey.isPressed) translation += Vector3.left;
        if (Keyboard.current.dKey.isPressed) translation += Vector3.right;

        transform.position += translation * Time.deltaTime * moveSpeed;
    }
}
