using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class DialogueEntry
{
    [TextArea(3, 10)]
    public string text;
    public string speakerName = "";
    public float displayDuration = 3f; // Auto-advance time (0 = manual advance)
}

[System.Serializable]
public class Dialogue
{
    public string dialogueID;
    public DialogueEntry[] entries;
    public bool hasBeenTriggered = false;
}

public class DialogueManager : MonoBehaviour
{
    [Header("Input Settings")]
    public InputActionReference advanceDialogueAction;
    public InputActionReference skipDialogueAction;
    
    [Header("UI References")]
    public GameObject dialoguePanel;            // Main dialogue UI panel
    public TextMeshProUGUI dialogueText;        // Text component for dialogue
    public TextMeshProUGUI speakerNameText;     // Text component for speaker name
    public Button nextButton;                   // Button to advance dialogue
    public Button skipButton;                   // Button to skip dialogue
    
    [Header("Animation Settings")]
    public float typewriterSpeed = 0.05f;       // Speed of typewriter effect
    public bool useTypewriterEffect = true;     // Enable/disable typewriter
    public bool enableAutoAdvance = false;      // Enable/disable auto-advance feature
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typingSound;               // Sound for each character
    public AudioClip dialogueStartSound;       // Sound when dialogue starts
    public AudioClip dialogueEndSound;         // Sound when dialogue ends
    
    // Singleton pattern
    public static DialogueManager Instance { get; private set; }
    
    private Queue<DialogueEntry> currentDialogue;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        currentDialogue = new Queue<DialogueEntry>();
    }
    
    void Start()
    {
        // Setup UI
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        if (nextButton != null)
            nextButton.onClick.AddListener(DisplayNextSentence);
            
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipDialogue);
            
        // Setup audio
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // Enable input actions
        if (advanceDialogueAction != null)
            advanceDialogueAction.action.Enable();
        if (skipDialogueAction != null)
            skipDialogueAction.action.Enable();
    }
    
    void OnDestroy()
    {
        // Disable input actions to prevent memory leaks
        if (advanceDialogueAction != null)
            advanceDialogueAction.action.Disable();
        if (skipDialogueAction != null)
            skipDialogueAction.action.Disable();
    }
    
    void Update()
    {
        if (!dialogueActive) return;
        
        // Check for advance dialogue input (Space/Enter)
        bool advancePressed = false;
        if (advanceDialogueAction != null)
        {
            advancePressed = advanceDialogueAction.action.WasPressedThisFrame();
        }
        
        // Check for skip dialogue input (Escape)
        bool skipPressed = false;
        if (skipDialogueAction != null)
        {
            skipPressed = skipDialogueAction.action.WasPressedThisFrame();
        }
        
        if (advancePressed)
        {
            if (isTyping)
            {
                // Skip typing animation
                SkipTyping();
            }
            else
            {
                // Advance to next dialogue
                DisplayNextSentence();
            }
        }
        
        if (skipPressed)
        {
            SkipDialogue();
        }
    }
    
    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogueActive) return; // Don't start new dialogue if one is active
        
        dialogueActive = true;
        currentDialogue.Clear();
        
        // Add all dialogue entries to queue
        foreach (DialogueEntry entry in dialogue.entries)
        {
            currentDialogue.Enqueue(entry);
        }
        
        // Show dialogue panel
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
            
        // Play start sound
        PlaySound(dialogueStartSound);
        
        // Pause the game (optional)
        Time.timeScale = 0f;
        
        DisplayNextSentence();
    }
    
    public void DisplayNextSentence()
    {
        // Stop auto-advance if it's running
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        if (currentDialogue.Count == 0)
        {
            EndDialogue();
            return;
        }
        
        DialogueEntry entry = currentDialogue.Dequeue();
        
        // Update speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = entry.speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(entry.speakerName));
        }
        
        // Stop current typing if active
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
            
        // Start typing animation
        if (useTypewriterEffect)
        {
            typingCoroutine = StartCoroutine(TypeSentence(entry.text));
        }
        else
        {
            dialogueText.text = entry.text;
            isTyping = false;
        }
        
        // Setup auto-advance if specified AND auto-advance is enabled
        if (enableAutoAdvance && entry.displayDuration > 0)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(entry.displayDuration));
        }
    }
    
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            
            // Play typing sound
            if (typingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(typingSound, 0.1f);
            }
            
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
        
        isTyping = false;
    }
    
    IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        // Wait for typing to finish
        while (isTyping)
            yield return null;
            
        // Wait for the specified duration
        yield return new WaitForSecondsRealtime(delay);
        
        // Auto-advance to next dialogue
        DisplayNextSentence();
    }
    
    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
            
            // Show full text immediately - we'll need to store current text
            // This is a limitation of the current implementation
        }
    }
    
    public void SkipDialogue()
    {
        // Clear remaining dialogue
        currentDialogue.Clear();
        
        // Stop all coroutines
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);
            
        EndDialogue();
    }
    
    void EndDialogue()
    {
        dialogueActive = false;
        isTyping = false;
        
        // Hide dialogue panel
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        // Play end sound
        PlaySound(dialogueEndSound);
        
        // Resume the game
        Time.timeScale = 1f;
        
        // Clear any remaining dialogue
        currentDialogue.Clear();
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public methods for external control
    public bool IsDialogueActive()
    {
        return dialogueActive;
    }
    
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = speed;
    }
    
    // Method to reset all dialogue triggers (useful for testing)
    public static void ResetAllTriggers()
    {
        DialogueTrigger[] triggers = FindObjectsOfType<DialogueTrigger>();
        foreach (DialogueTrigger trigger in triggers)
        {
            PlayerPrefs.DeleteKey($"DialogueTrigger_{trigger.triggerID}");
        }
        PlayerPrefs.Save();
    }
}