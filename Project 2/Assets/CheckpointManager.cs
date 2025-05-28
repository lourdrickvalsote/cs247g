// CheckpointManager.cs - Singleton to manage checkpoints
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    [Header("Checkpoint Settings")]
    public Vector3 currentCheckpoint = Vector3.zero;
    public bool hasCheckpoint = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetCheckpoint(Vector3 position)
    {
        currentCheckpoint = position;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint set at: {position}");
    }
    
    public Vector3 GetLastCheckpoint()
    {
        return hasCheckpoint ? currentCheckpoint : Vector3.zero;
    }
    
    public void RespawnPlayer()
    {
        if (hasCheckpoint)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // First, re-enable physics if they were disabled
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic)
                {
                    rb.isKinematic = false;
                }
                
                Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
                if (rb2d != null && !rb2d.simulated)
                {
                    rb2d.simulated = true;
                }
                
                // Set position
                player.transform.position = currentCheckpoint;
                
                // Now reset velocity (after physics are re-enabled)
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                if (rb2d != null)
                {
                    rb2d.linearVelocity = Vector2.zero;
                    rb2d.angularVelocity = 0f;
                }
                
                Debug.Log("Player respawned at checkpoint");
            }
        }
        else
        {
            Debug.LogWarning("No checkpoint available for respawn!");
        }
    }
}