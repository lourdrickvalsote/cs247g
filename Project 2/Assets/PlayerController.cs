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
    public LAYER currLayer = LAYER.back;  // initalize player location in midground
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
    public Animator animator;

    [Header("Effects")]
    public GameObject smokePrefab;          // Drag your smoke particle system prefab here
    public Transform smokeSpawnPoint;       // Optional: specific spawn point, if null uses player position
    public float smokeDestroyTime = 5f;     // How long before smoke is destroyed
    
    [Header("Running Effects")]
    public GameObject runningParticlesRightPrefab; // Particles for moving right
    public GameObject runningParticlesLeftPrefab;  // Particles for moving left
    public Transform feetPosition;                 // Position for feet particles (recommended)
    public float minSpeedForParticles = 0.1f;      // Minimum speed to show particles
    public Vector3 particleOffset = new Vector3(0f, -0.5f, 0f); // Base offset from player position
    public Vector3 rightMovementOffset = new Vector3(-0.3f, 0f, 0f); // Additional offset when moving right
    public Vector3 leftMovementOffset = new Vector3(0.3f, 0f, 0f);   // Additional offset when moving left

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
    
    // Running particles variables
    private GameObject activeRunningParticles;
    private bool wasMoving = false;
    private bool wasFacingRight = true; // Track previous facing direction

    void Start()
    {
        // Get components if not assigned
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

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
        bool facingRight = true;
        if (moveInput.x < 0)
        {
            sr.flipX = false;
            facingRight = false;
        }
        else if (moveInput.x > 0)
        {
            sr.flipX = true;
            facingRight = true;
        }
        else
        {
            // Not moving horizontally, keep previous direction
            facingRight = wasFacingRight;
        }

        // Update animations
        UpdateAnimations();

        // Handle running particles
        HandleRunningParticles(facingRight);

        // Update previous facing direction
        if (moveInput.x != 0)
        {
            wasFacingRight = facingRight;
        }

        // Detect ground each frame to update isGrounded state
        DetectGround();
    }

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

        // Correct Z position for layer system
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
            // SPAWN SMOKE EFFECT BEFORE LAYER CHANGE
            SpawnSmokeEffect();
            
            currentZ = rb.position.z + yPosition;
            Vector3 newPosition = rb.position;
            newPosition.z += yPosition;
            Debug.Log($"[LayerChange] Current Z: {rb.position.z}, yPosition: {yPosition}, New Z: {newPosition.z}");
            rb.MovePosition(newPosition);
            lastChangeTime = Time.time;
            moveInput.y = 0f;
        }
    }

    void SpawnSmokeEffect()
    {
        if (smokePrefab == null)
        {
            Debug.LogWarning("Smoke prefab not assigned in PlayerController!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition;
        if (smokeSpawnPoint != null)
        {
            spawnPosition = smokeSpawnPoint.position;
        }
        else
        {
            // Use player's position (you can add offset here if needed)
            spawnPosition = transform.position;
        }

        // Instantiate the smoke effect
        GameObject smokeInstance = Instantiate(smokePrefab, spawnPosition, Quaternion.identity);
        
        // Optional: Auto-destroy the smoke after set time to prevent scene clutter
        if (smokeDestroyTime > 0)
        {
            Destroy(smokeInstance, smokeDestroyTime);
        }
        
        Debug.Log($"Smoke spawned at position: {spawnPosition}");
    }

    void HandleRunningParticles(bool facingRight)
    {
        // Check if player is moving horizontally and is grounded
        bool isMoving = isGrounded && Mathf.Abs(moveInput.x) > minSpeedForParticles;

        if (isMoving && !wasMoving)
        {
            // Start running particles
            StartRunningParticles(facingRight);
        }
        else if (!isMoving && wasMoving)
        {
            // Stop running particles
            StopRunningParticles();
        }
        else if (isMoving && activeRunningParticles != null)
        {
            // Check if direction changed while moving
            if (facingRight != wasFacingRight)
            {
                // Direction changed, switch particle systems
                StopRunningParticles();
                StartRunningParticles(facingRight);
            }
            else
            {
                // Update particles position while running
                UpdateRunningParticlesPosition(facingRight);
            }
        }

        wasMoving = isMoving;
    }

    void StartRunningParticles(bool facingRight)
    {
        GameObject prefabToUse = facingRight ? runningParticlesRightPrefab : runningParticlesLeftPrefab;
        
        if (prefabToUse == null)
        {
            Debug.LogWarning($"Running particles prefab not assigned for {(facingRight ? "right" : "left")} direction!");
            return;
        }

        // Don't create new particles if they already exist
        if (activeRunningParticles != null)
            return;

        // Determine spawn position
        Vector3 spawnPosition = GetRunningParticlesPosition(facingRight);

        // Create running particles
        activeRunningParticles = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        
        Debug.Log($"Running particles started for {(facingRight ? "right" : "left")} direction");
    }

    void StopRunningParticles()
    {
        if (activeRunningParticles != null)
        {
            // Get particle system component to stop emission
            ParticleSystem ps = activeRunningParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Stop emission but let existing particles finish
                var emission = ps.emission;
                emission.enabled = false;
                
                // Destroy the GameObject after particles have time to fade out
                Destroy(activeRunningParticles, ps.main.startLifetime.constantMax + 1f);
            }
            else
            {
                // If no particle system found, destroy immediately
                Destroy(activeRunningParticles);
            }
            
            activeRunningParticles = null;
            Debug.Log("Running particles stopped");
        }
    }

    void UpdateRunningParticlesPosition(bool facingRight)
    {
        if (activeRunningParticles != null)
        {
            activeRunningParticles.transform.position = GetRunningParticlesPosition(facingRight);
        }
    }

    Vector3 GetRunningParticlesPosition(bool facingRight)
    {
        Vector3 position;
        
        if (feetPosition != null)
        {
            position = feetPosition.position;
        }
        else
        {
            // Use player position with base offset
            position = transform.position + particleOffset;
        }

        // Add directional offset based on movement direction
        Vector3 directionalOffset = facingRight ? rightMovementOffset : leftMovementOffset;
        position += directionalOffset;
        
        return position;
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

        // Stop running particles when jumping
        StopRunningParticles();

        // Animation trigger for jump
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