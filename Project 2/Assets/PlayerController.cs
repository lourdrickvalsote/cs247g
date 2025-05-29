using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum LAYER {back, mid, fore}
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpForce = 7f;         // Force applied when jumping
    public float jumpCooldown = 0.2f;    // Time before player can jump again
    public LAYER currLayer = LAYER.fore;  // initalize player location in midground
    float yPosition;
    public float layerCooldown = 0.2f;   // time before player can change layers again
    public float yDist = 10f;               // distance moved between layers

    [Header("Ground Detection")]
    public float groundDetectionHeight = 1f;    // Height to start the raycast from
    public float groundSnapDistance = 2f;       // Maximum distance to snap to ground
    public float groundOffset = 0.1f;           // Desired distance above ground
    public LayerMask terrainLayer;
    public bool debugGroundDetection = true;    // Enable visualization

    [Header("Components")]
    public Rigidbody rb;
    public SpriteRenderer sr;
    public Animator animator;  // ADD THIS LINE

    // Internal variables
    private Vector2 moveInput;
    private bool isGrounded;
    private RaycastHit groundHit;
    private bool jumpRequested;
    private float lastJumpTime;
    private bool layerChangeRequested;
    private float lastChangeTime;
    public bool dropRequested;
    public float currentZ = 0f;

    void Start()
    {
        // Get components if not assigned
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        // ADD THIS BLOCK
        if (animator == null)
            animator = GetComponent<Animator>();

        // Freeze rotation to prevent tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        transform.position = Vector3.zero;

        currentZ = rb.position.z;

        // Initial ground snap
        SnapToGroundImmediate();
    }

    void Update()
    {
        // Flip sprite based on movement direction
        if (moveInput.x < 0)
        {
            sr.flipX = false;
            // Debug.Log("face left");
        }
        else if (moveInput.x > 0)
        {
            sr.flipX = true;
            // Debug.Log("face right");
        }

        // ADD ANIMATION UPDATES HERE
        UpdateAnimations();

        // Detect ground each frame to update isGrounded state
        DetectGround();
    }

    // ADD THIS NEW METHOD
    void UpdateAnimations()
    {
        if (animator == null) return;

        // Update movement speed (for walk/run animations)
        float movementSpeed = Mathf.Abs(moveInput.x);
        animator.SetFloat("Speed", movementSpeed);

        // Update grounded state (for jump/land animations)
        animator.SetBool("IsGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        // Apply horizontal movement
        // Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 moveDirection = new Vector3(moveInput.x, 0, 0);
        Vector3 targetVelocity = moveDirection * speed;

        // Preserve current Y velocity (for gravity/falling)
        targetVelocity.y = rb.linearVelocity.y;

        // Apply velocity
        rb.linearVelocity = targetVelocity;

        // rail running, similar to jump requests
        if (layerChangeRequested)
        {
            performLayerChange();
            layerChangeRequested = false;
        }

        // Process jump requests
        if (jumpRequested)
        {
            PerformJump();
            jumpRequested = false;
        }

        // Apply ground snapping if needed
        if (isGrounded && Time.time > lastJumpTime + jumpCooldown)
        {
            SnapToGround();
        }

        // Debug.Log($"[FixedUpdate End] rb.position = {rb.position}, rb.velocity = {rb.linearVelocity}");
        Vector3 correctedPosition = rb.position;
        correctedPosition.z = currentZ;
        rb.MovePosition(correctedPosition);
    }

    void performLayerChange()
    {
        Debug.Log("attempting to perform layer change");
        int borderFlag = 0;
        if (moveInput.y > 0)
        {
            yPosition = 5f;
            if (currLayer == LAYER.mid)
            {
                Debug.Log("you're in the mid and want to go to the back");
                currLayer = LAYER.back;
            }
            else if (currLayer == LAYER.fore)
            {
                Debug.Log("you're in the fore and want to go to the mid");
                currLayer = LAYER.mid;
            }
            else
            {
                Debug.Log("you're in the back and want to go further back, not allowed!");
                borderFlag = 1;
            }
            Debug.Log($"y-direction intended in if-statement: {yPosition}");

        }
        else if (moveInput.y < 0)
        {
            yPosition = -5f;
            if (currLayer == LAYER.mid)
            {
                Debug.Log("you're in the mid and want to go to the fore");
                currLayer = LAYER.fore;
            }
            else if (currLayer == LAYER.back)
            {
                Debug.Log("you're in the back and want to go to the mid");
                currLayer = LAYER.mid;
            }
            else
            {
                Debug.Log("you're in the fore and want to go further forward, not allowed!");
                borderFlag = 1;
            }
            Debug.Log($"y-direction intended in if-statement: {yPosition}");
        }
        if (borderFlag == 1)
        {
            // do nothing, move is disallowed
        }
        else
        {
            // Vector3 newPosition = (rb.position.z + yPosition) * Vector3.forward;
            // newPosition.y = rb.position.y;
            // newPosition.x = rb.position.x;
            // rb.MovePosition(newPosition);
            currentZ = rb.position.z + yPosition;
            Vector3 newPosition = rb.position;
            newPosition.z += yPosition;
            Debug.Log($"[LayerChange] Current Z: {rb.position.z}, yPosition: {yPosition}, New Z: {newPosition.z}");
            rb.MovePosition(newPosition);
            lastChangeTime = Time.time;
            moveInput.y = 0f;
        }
    }

    public void OnLayerChange(InputAction.CallbackContext context)
    {
        // register layer change on button press
        if (context.started)
        {
            Debug.Log("changing depth layer");
            float value = context.ReadValue<float>();
            // queue layer change only if cooldown passed
            if (Time.time > lastChangeTime + layerCooldown && Mathf.Abs(value) > 0.1f)
            {
                moveInput.y = Mathf.Sign(value);
                layerChangeRequested = true;
                Debug.Log("Layer change requested");
            }
        }
    }

    public void OnPlatformDrop(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            dropRequested = true;
            Debug.Log("Platform drop requested");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Only register jump on button press, not release
        if (context.started)
        {
            // Queue jump request if we're grounded
            if (isGrounded && Time.time > lastJumpTime + jumpCooldown)
            {
                jumpRequested = true;

                if (debugGroundDetection)
                {
                    Debug.Log("Jump requested");
                }
            }
        }
    }

    void PerformJump()
    {
        // Apply jump force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Update last jump time
        lastJumpTime = Time.time;

        // Set grounded to false immediately
        isGrounded = false;

        // ADD ANIMATION TRIGGER FOR JUMP
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        if (debugGroundDetection)
        {
            Debug.Log($"Jump performed with force: {jumpForce}");
        }
    }

    bool DetectGround()
    {
        // Cast a ray from slightly above the character
        Vector3 rayOrigin = transform.position + Vector3.up * groundDetectionHeight;

        // Check if we hit ground within our snap distance
        bool hitGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out groundHit,
            groundDetectionHeight + groundSnapDistance,
            terrainLayer
        );

        // Update grounded state
        isGrounded = hitGround;

        // Visualize the ground detection
        if (debugGroundDetection)
        {
            Color rayColor = hitGround ? Color.green : Color.red;
            Debug.DrawRay(rayOrigin, Vector3.down * (groundDetectionHeight + groundSnapDistance), rayColor);

            if (hitGround)
            {
                Debug.DrawLine(groundHit.point, groundHit.point + Vector3.up * groundOffset, Color.yellow);
            }
        }

        return hitGround;
    }

    void SnapToGroundImmediate()
    {
        if (DetectGround())
        {
            // Safe to directly set position before physics is active
            Vector3 newPosition = transform.position;
            newPosition.y = groundHit.point.y + groundOffset;
            transform.position = newPosition;

            if (debugGroundDetection)
            {
                Debug.Log($"Initial ground snap to Y: {newPosition.y}");
            }
        }
    }

    void SnapToGround()
    {
        // Only snap if we're grounded
        if (!isGrounded) return;

        // Calculate target Y position
        float targetY = groundHit.point.y + groundOffset;

        // Calculate current distance from desired position
        float currentDistance = Mathf.Abs(rb.position.y - targetY);

        if (currentDistance > groundOffset * 1.5f)
        {
            // For large distances, use MovePosition for smoother transition
            Vector3 targetPosition = rb.position;
            targetPosition.y = targetY;

            // Use interpolation for smoother snapping
            float snapSpeed = 10f;
            Vector3 smoothedPosition = Vector3.Lerp(rb.position, targetPosition, Time.fixedDeltaTime * snapSpeed);
            rb.MovePosition(smoothedPosition);

            if (debugGroundDetection)
            {
                Debug.Log($"Snapping to ground: Target Y={targetY}, Current Y={rb.position.y}, Distance={currentDistance}");
            }
        }
    }
}
