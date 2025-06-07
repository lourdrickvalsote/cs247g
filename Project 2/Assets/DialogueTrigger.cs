using UnityEngine;
using System.Collections.Generic;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string triggerID;                    // Unique ID for this trigger
    public bool triggerOnce = true;             // Only trigger the first time
    public TriggerPersistence persistence = TriggerPersistence.SessionOnly;
    public LayerMask playerLayer = 1;           // Which layer is the player on
    
    [Header("Dialogue")]
    public Dialogue dialogue;
    
    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;
    
    // Static list to track triggered dialogues in current session
    private static HashSet<string> sessionTriggeredDialogues = new HashSet<string>();
    
    private bool hasTriggered = false;
    
    void Start()
    {
        // Load trigger state based on persistence setting
        if (triggerOnce)
        {
            switch (persistence)
            {
                case TriggerPersistence.SessionOnly:
                    hasTriggered = sessionTriggeredDialogues.Contains(triggerID);
                    break;
                    
                case TriggerPersistence.Permanent:
                    hasTriggered = PlayerPrefs.GetInt($"DialogueTrigger_{triggerID}", 0) == 1;
                    break;
                    
                case TriggerPersistence.Never:
                    hasTriggered = false; // Always allow triggering
                    break;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player and hasn't been triggered yet
        if (((1 << other.gameObject.layer) & playerLayer) != 0 && (!triggerOnce || !hasTriggered))
        {
            TriggerDialogue();
        }
    }
    
    void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
            
            if (triggerOnce)
            {
                hasTriggered = true;
                
                switch (persistence)
                {
                    case TriggerPersistence.SessionOnly:
                        // Add to session list (resets when game restarts)
                        sessionTriggeredDialogues.Add(triggerID);
                        break;
                        
                    case TriggerPersistence.Permanent:
                        // Save to PlayerPrefs (persists between sessions)
                        PlayerPrefs.SetInt($"DialogueTrigger_{triggerID}", 1);
                        PlayerPrefs.Save();
                        break;
                        
                    case TriggerPersistence.Never:
                        // Don't save anywhere - always allow retriggering
                        break;
                }
            }
        }
        else
        {
            Debug.LogWarning("DialogueManager not found in scene!");
        }
    }
    
    void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = hasTriggered ? Color.gray : gizmoColor;
            
            // Draw trigger area
            if (GetComponent<BoxCollider>())
            {
                BoxCollider box = GetComponent<BoxCollider>();
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (GetComponent<SphereCollider>())
            {
                SphereCollider sphere = GetComponent<SphereCollider>();
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
    
    // Public method to reset this specific trigger
    public void ResetTrigger()
    {
        hasTriggered = false;
        sessionTriggeredDialogues.Remove(triggerID);
        PlayerPrefs.DeleteKey($"DialogueTrigger_{triggerID}");
    }
    
    // Static method to reset all session triggers
    public static void ResetAllSessionTriggers()
    {
        sessionTriggeredDialogues.Clear();
    }
    
    // Static method to reset all permanent triggers
    public static void ResetAllPermanentTriggers()
    {
        DialogueTrigger[] triggers = FindObjectsOfType<DialogueTrigger>();
        foreach (DialogueTrigger trigger in triggers)
        {
            PlayerPrefs.DeleteKey($"DialogueTrigger_{trigger.triggerID}");
        }
        PlayerPrefs.Save();
    }
}

public enum TriggerPersistence
{
    SessionOnly,    // Resets when game is restarted (default for testing)
    Permanent,      // Persists between game sessions (for final game)
    Never          // Always allows triggering (for repeatable dialogues)
}