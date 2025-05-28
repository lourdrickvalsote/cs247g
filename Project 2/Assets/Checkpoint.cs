// Checkpoint.cs - Individual checkpoint behavior
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public PlayerController controller;
    [Header("Checkpoint Visuals")]
    public GameObject activeVisual;   // Object to show when checkpoint is active
    public GameObject inactiveVisual; // Object to show when checkpoint is inactive
    
    [Header("Lane Settings")]
    public float respawnZPosition = 0f; // Set this to specify which lane to respawn in
    public LAYER checkpointLayer;
    
    [Header("Effects")]
    public ParticleSystem activationEffect;
    public AudioClip activationSound;
    
    private bool isActivated = false;
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        UpdateVisuals();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }
    
    // For 2D colliders
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }
    
    private void ActivateCheckpoint()
    {
        isActivated = true;
        
        // Create respawn position using checkpoint's X,Y and specified Z (lane)
        Vector3 respawnPosition = new Vector3(transform.position.x, transform.position.y, respawnZPosition);
        
        // Set this as the current checkpoint
        CheckpointManager.Instance.SetCheckpoint(respawnPosition);
        checkpointLayer = controller.currLayer;
        
        // Deactivate all other checkpoints
        DeactivateOtherCheckpoints();
        
        // Update visuals
        UpdateVisuals();
        
        // Play effects
        PlayActivationEffects();
    }
    
    private void DeactivateOtherCheckpoints()
    {
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint checkpoint in allCheckpoints)
        {
            if (checkpoint != this)
            {
                checkpoint.SetInactive();
            }
        }
    }
    
    public void SetInactive()
    {
        isActivated = false;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
        
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);
    }
    
    private void PlayActivationEffects()
    {
        // Play particle effect
        if (activationEffect != null)
            activationEffect.Play();
        
        // Play sound
        if (audioSource != null && activationSound != null)
            audioSource.PlayOneShot(activationSound);
    }
}
