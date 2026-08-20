using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 720f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // prevent physics from tipping the player over
    }

    void Update()
    {
        ReadInput();
        UpdateFacingDirection();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void ReadInput()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current == null) return;

        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

        if (Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        // Get camera-relative directions
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
    }

    void UpdateFacingDirection()
    {
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void ApplyMovement()
    {
        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}