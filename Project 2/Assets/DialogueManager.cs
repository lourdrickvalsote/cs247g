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
    public bool enablePopupAnimation = true;    // Enable/disable popup animations
    public float popupDuration = 0.3f;          // How long the popup animation takes
    public AnimationCurve popupCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)); // Animation curve
    public float slideDistance = 500f;          // Distance to slide from bottom (in pixels)
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typingSound;               // Sound for each character
    public AudioClip dialogueStartSound;       // Sound when dialogue starts
    public AudioClip dialogueEndSound;         // Sound when dialogue ends
    public float audioVolume = 1f;              // Volume for dialogue sounds
    public bool pauseGameDuringDialogue = false; // Don't pause physics - keep game running
    
    // Singleton pattern
    public static DialogueManager Instance { get; private set; }
    
    private Queue<DialogueEntry> currentDialogue;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private Coroutine popupCoroutine;  // For popup animations
    
    // Animation variables
    private RectTransform dialogueRectTransform;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;
    
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
        // Setup UI - ENSURE DIALOGUE PANEL IS HIDDEN
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("Dialogue panel set to inactive");
        }
        else
        {
            Debug.LogError("Dialogue panel is not assigned in DialogueManager!");
        }
            
        if (nextButton != null)
            nextButton.onClick.AddListener(DisplayNextSentence);
            
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipDialogue);
            
        // Setup audio - IMPROVED AUDIO SETUP
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Created new AudioSource for DialogueManager");
            }
        }
        
        // Configure AudioSource for dialogue
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.volume = audioVolume;
        
        Debug.Log($"AudioSource configured: {audioSource != null}");
        
        // Setup slide animation
        if (dialoguePanel != null)
        {
            dialogueRectTransform = dialoguePanel.GetComponent<RectTransform>();
            if (dialogueRectTransform != null)
            {
                // Store the original position (where dialogue should appear)
                originalPosition = dialogueRectTransform.anchoredPosition;
                // Calculate hidden position (below screen)
                hiddenPosition = new Vector2(originalPosition.x, originalPosition.y - slideDistance);
                
                // Start in hidden position
                dialogueRectTransform.anchoredPosition = hiddenPosition;
                
                Debug.Log($"Slide animation setup - Original: {originalPosition}, Hidden: {hiddenPosition}");
            }
        }
            
        // Enable input actions
        if (advanceDialogueAction != null)
            advanceDialogueAction.action.Enable();
        if (skipDialogueAction != null)
            skipDialogueAction.action.Enable();
            
        // IMPORTANT: Ensure dialogue is not active at start
        dialogueActive = false;
        isTyping = false;
        
        // Keep time scale normal - don't pause the game
        Time.timeScale = 1f;
        
        Debug.Log($"DialogueManager initialized. Dialogue active: {dialogueActive}");
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
        
        // Stop player's horizontal movement when dialogue starts
        StopPlayerMovement();
        
        // Add all dialogue entries to queue
        foreach (DialogueEntry entry in dialogue.entries)
        {
            currentDialogue.Enqueue(entry);
        }
        
        // Show dialogue panel with animation
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            if (enablePopupAnimation)
            {
                StartPopupAnimation();
            }
        }
            
        // Play start sound (no need to worry about pausing)
        PlaySound(dialogueStartSound);
        
        // Don't pause the game - let physics continue
        // Player input is blocked by PlayerController checking IsDialogueActive()
        
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
                audioSource.PlayOneShot(typingSound, audioVolume * 0.3f); // Lower volume for typing
            }
            
            yield return new WaitForSeconds(typewriterSpeed); // Use regular WaitForSeconds since game isn't paused
        }
        
        isTyping = false;
    }
    
    IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        // Wait for typing to finish
        while (isTyping)
            yield return null;
            
        // Wait for the specified duration
        yield return new WaitForSeconds(delay); // Use regular WaitForSeconds since game isn't paused
        
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
        if (enablePopupAnimation && dialoguePanel != null)
        {
            // Start popup down animation, then hide panel
            StartPopdownAnimation();
        }
        else
        {
            // Immediate hide without animation
            CompleteDialogueEnd();
        }
    }
    
    // ANIMATION METHODS:
    void StartPopupAnimation()
    {
        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);
            
        popupCoroutine = StartCoroutine(PopupAnimation());
    }
    
    void StartPopdownAnimation()
    {
        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);
            
        popupCoroutine = StartCoroutine(PopdownAnimation());
    }
    
    IEnumerator PopupAnimation()
    {
        if (dialogueRectTransform == null) yield break;
        
        // Start from hidden position (below screen)
        dialogueRectTransform.anchoredPosition = hiddenPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < popupDuration)
        {
            elapsedTime += Time.deltaTime; // Use regular deltaTime since game isn't paused
            float progress = elapsedTime / popupDuration;
            
            // Apply animation curve for smooth easing
            float curveValue = popupCurve.Evaluate(progress);
            
            // Lerp from hidden position to original position
            Vector2 currentPosition = Vector2.Lerp(hiddenPosition, originalPosition, curveValue);
            dialogueRectTransform.anchoredPosition = currentPosition;
            
            yield return null;
        }
        
        // Ensure final position is exactly the original position
        dialogueRectTransform.anchoredPosition = originalPosition;
    }
    
    IEnumerator PopdownAnimation()
    {
        if (dialogueRectTransform == null) yield break;
        
        // Start from current position (should be original position)
        Vector2 startPosition = dialogueRectTransform.anchoredPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < popupDuration)
        {
            elapsedTime += Time.deltaTime; // Use regular deltaTime since game isn't paused
            float progress = elapsedTime / popupDuration;
            
            // Apply animation curve for smooth easing
            float curveValue = popupCurve.Evaluate(progress);
            
            // Lerp from current position to hidden position
            Vector2 currentPosition = Vector2.Lerp(startPosition, hiddenPosition, curveValue);
            dialogueRectTransform.anchoredPosition = currentPosition;
            
            yield return null;
        }
        
        // Ensure final position is exactly the hidden position
        dialogueRectTransform.anchoredPosition = hiddenPosition;
        
        // Complete the dialogue ending
        CompleteDialogueEnd();
    }
    
    void CompleteDialogueEnd()
    {
        dialogueActive = false;
        isTyping = false;
        
        // Play end sound
        PlaySound(dialogueEndSound);
        
        // Hide dialogue panel
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        // Game continues running - no need to resume time scale
        // Physics and gravity continue working normally
        
        // Clear any remaining dialogue
        currentDialogue.Clear();
        
        // Reset position in case animation was interrupted
        if (dialogueRectTransform != null)
            dialogueRectTransform.anchoredPosition = hiddenPosition;
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null - cannot play sound");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is null - cannot play sound");
            return;
        }
        
        // Play the sound
        audioSource.PlayOneShot(clip, audioVolume);
        Debug.Log($"Playing sound: {clip.name} at volume {audioVolume}");
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
    
    // DEBUG METHOD - Add this for troubleshooting
    void LateUpdate()
    {
        // Debug info to help diagnose issues
        if (Input.GetKeyDown(KeyCode.F1)) // Press F1 for debug info
        {
            Debug.Log($"=== DIALOGUE MANAGER DEBUG ===");
            Debug.Log($"Dialogue Active: {dialogueActive}");
            Debug.Log($"Is Typing: {isTyping}");
            Debug.Log($"Time Scale: {Time.timeScale}");
            Debug.Log($"Dialogue Panel Active: {(dialoguePanel != null ? dialoguePanel.activeInHierarchy.ToString() : "NULL")}");
            Debug.Log($"Current Dialogue Count: {currentDialogue.Count}");
        }
        
        // Emergency reset - Press F2 to force reset dialogue system
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("EMERGENCY RESET: Forcing dialogue system reset");
            ForceResetDialogue();
        }
    }
    
    public void ForceResetDialogue()
    {
        // Force reset everything
        dialogueActive = false;
        isTyping = false;
        // Don't need to reset Time.timeScale since we're not pausing the game
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            // Reset position in case animation was interrupted
            if (dialogueRectTransform != null)
                dialogueRectTransform.anchoredPosition = hiddenPosition;
        }
            
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
        
        currentDialogue.Clear();
        
        Debug.Log("Dialogue system force reset complete");
    }
    
    // Method to stop player's horizontal movement when dialogue starts
    void StopPlayerMovement()
    {
        // Find the player controller and stop horizontal movement
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // Call the player's method to stop horizontal movement
            player.StopHorizontalMovement();
        }
        else
        {
            Debug.LogWarning("PlayerController not found - cannot stop movement");
        }
    }
}