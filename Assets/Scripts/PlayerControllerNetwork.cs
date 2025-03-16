using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
public class PlayerControllerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject _cameraHolder;

    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private Vector2 _input;
    private Vector3 _direction;

    public float _gravity = -9.8f;
    public float _gravityMultipler = 1.0f;
    public float fallSpeed;

    private float rotationVelocity;
    public float rotationSmoothTime = 0.2f;

    public float jumpPower = 5.0f;
    public float moveSpeed = 3.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];

        _moveAction.performed += Move;
        _moveAction.canceled += Move;

        _jumpAction.started += Jump;

        if (!IsOwner)
        {
            this.enabled = false;
            return;
        } else
        {
            _cameraHolder.SetActive(true);
        }

    }

    private void Update()
    {
        HandleGravity();
        UpdateDirection();
        HandleMovement();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (_input.sqrMagnitude == 0) return;

        float targetAngle = Mathf.Atan2(_input.x, _input.y) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    private void HandleGravity()
    {
        if (IsGrounded() && fallSpeed <= -1) fallSpeed = -1;
        else fallSpeed += _gravity * _gravityMultipler * Time.deltaTime;
    }
    private void UpdateDirection()
    {
        _direction.x = _input.x;
        _direction.y = fallSpeed;
        _direction.z = _input.y;
    }

    private void HandleMovement()
    {
        _characterController.Move(_direction * Time.deltaTime * moveSpeed);
    }

    private void Move(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!IsGrounded()) return;

        fallSpeed = jumpPower;
    }

    private bool IsGrounded() => _characterController.isGrounded;

    private void OnDisable()
    {
        _moveAction.performed -= Move;
        _moveAction.canceled -= Move;

        _jumpAction.started -= Jump;
    }
}
