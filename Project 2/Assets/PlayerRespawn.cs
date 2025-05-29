// PlayerRespawn.cs - Handle player death and respawning
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public Checkpoint checkpoint;
    public PlayerController controller;

    [Header("Death Settings")]
    public float fallThreshold = -10f; // Y position below which player dies
    public float respawnDelay = 1f;
    public int deathCounter = 0;
    
    [Header("Effects")]
    public ParticleSystem deathEffect;
    public AudioClip deathSound;
    
    private AudioSource audioSource;
    private bool isDead = false;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    private void Update()
    {
        // Check if player has fallen below the death threshold
        if (!isDead && transform.position.y < fallThreshold)
        {
            Die();
            deathCounter = deathCounter + 1;
        }
    }
    
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Play death effects
        if (deathEffect != null)
            deathEffect.Play();
        
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
        
        // Disable player controls/movement
        DisablePlayerControls();
        
        // Respawn after delay
        Invoke(nameof(Respawn), respawnDelay);
    }
    
    private void Respawn()
    {
        //PlayerController.currentZ = CheckpointManager.Instance.GetLastCheckpoint().z;
        //float checkpoint = CheckpointManager.Instance.GetLastCheckpoint().z;
        //PlayerController.currentZ = checkpoint;
        //PlayerController.currentZ = Checkpoint.respawnZPosition;
        controller.currentZ = checkpoint.respawnZPosition;
        controller.currLayer = checkpoint.checkpointLayer;
        CheckpointManager.Instance.RespawnPlayer();
        isDead = false;
        
        // Re-enable player controls
        EnablePlayerControls();
    }
    
    private void DisablePlayerControls()
    {
        // Disable player movement script if you have one
        var playerController = GetComponent<PlayerController>(); // Replace with your player controller script
        if (playerController != null)
        {
            // playerController.enabled = false;
        }
        
        // Stop player physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.simulated = false;
        }
    }
    
    private void EnablePlayerControls()
    {
        // Re-enable player movement script
        var playerController = GetComponent<MonoBehaviour>(); // Replace with your player controller script
        if (playerController != null)
        {
            // playerController.enabled = true;
        }
        
        // Resume player physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.simulated = true;
        }
    }
    
    // Public method to trigger death from other scripts (like damage zones)
    public void TriggerDeath()
    {
        Die();
    }
}