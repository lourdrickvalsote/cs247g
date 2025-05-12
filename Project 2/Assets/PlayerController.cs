using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    
    [Header("Ground Detection")]
    public float groundDetectionHeight = 1f;    // Height to start the raycast from
    public float groundSnapDistance = 2f;       // Maximum distance to snap to ground
    public float groundOffset = 0.1f;           // Desired distance above ground
    public LayerMask terrainLayer;
    public bool debugGroundDetection = true;    // Enable visualization
    
    [Header("Components")]
    public Rigidbody rb;
    public SpriteRenderer sr;

    // Internal variables
    private Vector2 moveInput;
    private bool isGrounded;
    private RaycastHit groundHit;

    void Start()
    {
        // Get components if not assigned
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        // Freeze rotation to prevent tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Initial ground snap
        SnapToGroundImmediate();
    }

    void Update()
    {
        // Flip sprite based on movement direction
        if (moveInput.x < 0)
            sr.flipX = false;
        else if (moveInput.x > 0)
            sr.flipX = true;
            
        // Detect ground each frame to update isGrounded state
        DetectGround();
    }

    void FixedUpdate()
    {
        // Apply horizontal movement
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 targetVelocity = moveDirection * speed;
        
        // Preserve current Y velocity (for gravity/falling)
        targetVelocity.y = rb.linearVelocity.y;
        
        // Apply velocity
        rb.linearVelocity = targetVelocity;
        
        // Apply ground snapping if needed
        if (isGrounded)
        {
            SnapToGround();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
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