using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class TutorialStep
{
    public string title;
    public string description;
    public GameObject elementToHighlight; // the actual UI GameObject (button, panel, etc.)
    public bool waitForUserInteraction;
    public string interactionTarget; // "Capture","Gallery","Instructions","CloseResults" etc.
    public bool allowRealFunctionality;
}

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial UI (scene)")]
    public GameObject tutorialPanel;      // Canvas/TutorialPanel
    public Image panelBackground;         // TutorialPanel/PanelBackground (Image)
    public TMP_Text titleText;            // TutorialPanel/TitleText
    public TMP_Text descriptionText;      // TutorialPanel/DescriptionText
    public Button btnInsNext;             // TutorialPanel/BTN_InsNext
    public Button btnInsSkip;             // TutorialPanel/BTN_InsSkip

    [Header("App Buttons (scene)")]
    public Button captureButton;      // Main_Components/BTN_Capture
    public Button galleryButton;      // Main_Components/BTN_Gallery
    public Button instructionsButton; // Main_Components/BTN_Instructions
    public Button retryButton;        // Main_Components/BTN_RetryCapture
    public Button exitScanButton;     // Canvas/BTN_ExitScan
    public GameObject resultsPanel;   // ResultsPanel

    [Header("Camera Preview Cover")]
    public Image cameraPreviewCover;  // Add this to cover camera preview during tutorial

    [Header("Animation Config")]
    public float fadeDuration = 0.25f;
    public float pulseScaleAmount = 1.1f;
    public float pulseDuration = 0.8f;

    // internal
    private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    private int currentIndex = 0;
    private bool tutorialActive = false;

    // To restore reparented elements
    private class SavedTransformInfo { public Transform parent; public int siblingIndex; }
    private Dictionary<Transform, SavedTransformInfo> savedTransforms = new Dictionary<Transform, SavedTransformInfo>();

    // Used to hold temporary event listeners so we can remove them safely
    private List<Action> temporaryUnsubs = new List<Action>();

    // Store original button states and listeners
    private bool originalCaptureInteractable = true;
    private bool originalGalleryInteractable = true;
    private bool originalInstructionsInteractable = true;

    // Animation coroutine reference
    private Coroutine currentPulseCoroutine;

    void Start()
    {
        // bind control buttons
        btnInsNext.onClick.AddListener(OnNextClicked);
        btnInsSkip.onClick.AddListener(EndTutorialImmediate);

        // instructions button replays tutorial
        instructionsButton.onClick.AddListener(() =>
        {
            if (!tutorialActive) StartCoroutine(StartTutorialSequence());
        });

        // Build steps and optionally auto-start if first time
        BuildTutorialSteps();

        if (!PlayerPrefs.HasKey("TutorialCompleted"))
            StartCoroutine(StartTutorialSequence());
        else
        {
            // make sure tutorial UI is off by default
            tutorialPanel.SetActive(false);
            panelBackground.gameObject.SetActive(false);
            if (cameraPreviewCover != null) cameraPreviewCover.gameObject.SetActive(false);
        }
    }

    public bool IsTutorialActive()
    {
        return tutorialActive;
    }

    void BuildTutorialSteps()
    {
        tutorialSteps.Clear();

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>WELCOME TO NUTRIVENTURE</u>",
            description = "Your mission: <color=#4ECDC4>discover ingredients in real products</color>. Let's guide you through your first scan!",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "STEP 1:\n<u>CAPTURE INGREDIENTS</u>",
            description = "Tap the <color=#FF6B6B>CAPTURE</color> button to take a live photo and start scanning.",
            elementToHighlight = captureButton != null ? captureButton.gameObject : null,
            waitForUserInteraction = true,
            interactionTarget = "Capture",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>GREAT JOB!</u>",
            description = "You captured an image — the app will <color=#4ECDC4>process it and show the results</color>.",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>UNDERSTANDING RESULTS</u>",
            description = "Results show: <color=white>ingredient name</color>, <color=#6BCEFF>category & rarity</color>, <color=#FFD93D>unlock chance</color>, and a <color=#FF6B6B>3D preview</color>.",
            elementToHighlight = resultsPanel,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "STEP 2:\n<u>CLOSE RESULTS</u>",
            description = "Tap the <color=#FF6B6B>Exit Scan</color> button to return to scanning mode.",
            elementToHighlight = exitScanButton != null ? exitScanButton.gameObject : null,
            waitForUserInteraction = true,
            interactionTarget = "CloseResults",
            allowRealFunctionality = true
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>EXCELLENT!</u>",
            description = "You know how to <color=#4ECDC4>close results and return to scanning</color>.",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "STEP 3:\n<u>GALLERY OPTION</u>",
            description = "Tap the <color=#FF6B6B>GALLERY</color> button to choose an existing photo and scan it.",
            elementToHighlight = galleryButton != null ? galleryButton.gameObject : null,
            waitForUserInteraction = true,
            interactionTarget = "Gallery",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>PERFECT!</u>",
            description = "You can now scan using <color=#4ECDC4>both camera and gallery</color>.",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "STEP 4:\n<u>GETTING HELP</u>",
            description = "If you forget anything, tap the <color=#FF6B6B>INSTRUCTIONS</color> button to replay this tutorial anytime.",
            elementToHighlight = instructionsButton != null ? instructionsButton.gameObject : null,
            waitForUserInteraction = true,
            interactionTarget = "Instructions",
            allowRealFunctionality = true
        });

        // ============ COLOR-CODED PRO TIPS ============

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>PRO TIP: SMART SCANNING</u>",
            description = "Scan each product <color=#FFD93D>3 times</color> to collect all ingredients. Use the <color=#6BCEFF>10-second cooldown</color> to check your collection, and remember products reset <color=#4ECDC4>daily</color> for fresh scanning opportunities!",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>PRO TIP: RARITY SYSTEM</u>",
            description = "Ingredients come in <color=white>Common</color>, <color=blue>Rare</color>, and <color=#FF6B6B>Ultra Rare</color> rarities. The system analyzes product complexity and ingredient distribution to determine what you discover!",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>PRO TIP: COLLECTION GOALS</u>",
            description = "Complete all four categories - <color=#FF6B6B>Preservatives</color>, <color=#FFD93D>Sweeteners</color>, <color=#6BCEFF>Fortificants</color>, and <color=#FFD700>Allergens</color> - to earn special rewards!",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });

        tutorialSteps.Add(new TutorialStep
        {
            title = "<u>TUTORIAL COMPLETE!</u>",
            description = "<color=#4ECDC4>Congratulations!</color> You're ready to scan products, discover ingredients and build your collection. <color=#FFD93D>Happy scanning!</color>",
            elementToHighlight = null,
            waitForUserInteraction = false,
            interactionTarget = "",
            allowRealFunctionality = false
        });
    }

    IEnumerator StartTutorialSequence()
    {
        tutorialActive = true;

        // ADD THIS: Set tutorial flag in OCRManager
        OCRManager ocrManager = FindFirstObjectByType<OCRManager>();
        if (ocrManager != null)
        {
            ocrManager.IsTutorialActive = true;
        }

        savedTransforms.Clear();
        temporaryUnsubs.Clear();
        currentIndex = 0;

        // Store original button states
        originalCaptureInteractable = captureButton.interactable;
        originalGalleryInteractable = galleryButton.interactable;
        originalInstructionsInteractable = instructionsButton.interactable;

        // Show camera preview cover during tutorial and position it properly
        if (cameraPreviewCover != null)
        {
            cameraPreviewCover.gameObject.SetActive(true);
            cameraPreviewCover.color = new Color(0, 0, 0, 0.8f);
            if (captureButton != null && captureButton.transform.parent != null)
            {
                cameraPreviewCover.transform.SetParent(captureButton.transform.parent, false);
                cameraPreviewCover.transform.SetAsFirstSibling();
            }
        }

        // show overlay
        panelBackground.gameObject.SetActive(true);
        tutorialPanel.SetActive(true);
        panelBackground.color = new Color(panelBackground.color.r, panelBackground.color.g, panelBackground.color.b, 0.9f);

        // fade in overlay alpha
        yield return StartCoroutine(FadeOverlay(0f, 0.3f));

        ShowCurrentStep();
    }

    void ShowCurrentStep()
    {
        if (currentIndex < 0 || currentIndex >= tutorialSteps.Count) { EndTutorial(); return; }
        var step = tutorialSteps[currentIndex];

        // Stop any existing pulse animation
        if (currentPulseCoroutine != null)
        {
            StopCoroutine(currentPulseCoroutine);
            currentPulseCoroutine = null;
        }

        // update text
        titleText.text = step.title;
        descriptionText.text = step.description;

        // Hide Next button for interactive steps, show for informational steps
        bool showNextButton = !step.waitForUserInteraction;
        btnInsNext.gameObject.SetActive(showNextButton);

        // FIX: ALWAYS ensure Skip button is visible and interactable
        btnInsSkip.gameObject.SetActive(true);
        btnInsSkip.interactable = true;

        // CRITICAL FIX: Ensure both Next and Skip buttons are clickable by bringing them to front
        btnInsNext.transform.SetAsLastSibling();
        btnInsSkip.transform.SetAsLastSibling();

        // Ensure tutorialPanel (text & next/skip) stays on top of everything
        tutorialPanel.transform.SetAsLastSibling();

        // If there's an element to emphasize, reparent it under tutorialPanel
        if (step.elementToHighlight != null)
        {
            BringElementAboveBackground(step.elementToHighlight);
            // Start pulse animation on the highlighted element
            currentPulseCoroutine = StartCoroutine(PulseAnimation(step.elementToHighlight));
        }

        // ALWAYS keep panel background active, visible and blocking during tutorial steps
        panelBackground.gameObject.SetActive(true);
        panelBackground.raycastTarget = true;
        panelBackground.color = new Color(panelBackground.color.r, panelBackground.color.g, panelBackground.color.b, 0.9f);

        // FIX: Ensure Skip button is always accessible
        SetupSkipButtonAccess();

        // Disable other buttons during tutorial to prevent double functionality
        UpdateButtonInteractability(step);

        // If step waits for user interaction, set up the appropriate handler
        if (step.waitForUserInteraction)
        {
            if (step.allowRealFunctionality)
            {
                // For steps that allow real functionality, temporarily disable background blocking
                panelBackground.raycastTarget = false;
                StartCoroutine(WaitForUserAction(step.interactionTarget));
            }
            else
            {
                // Use tutorial simulation for capture and gallery - keep background blocking
                StartCoroutine(WaitForTutorialAction(step.interactionTarget));
            }
        }
    }

    void SetupSkipButtonAccess()
    {
        // Ensure Skip button is always on top and clickable
        btnInsSkip.transform.SetAsLastSibling();

        // Make sure the button has a higher sort order
        Canvas skipCanvas = btnInsSkip.GetComponent<Canvas>();
        if (skipCanvas == null)
        {
            skipCanvas = btnInsSkip.gameObject.AddComponent<Canvas>();
        }
        skipCanvas.overrideSorting = true;
        skipCanvas.sortingOrder = 100; // Very high number to ensure it's on top

        // Also add a GraphicRaycaster if missing
        GraphicRaycaster skipRaycaster = btnInsSkip.GetComponent<GraphicRaycaster>();
        if (skipRaycaster == null)
        {
            skipRaycaster = btnInsSkip.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    IEnumerator PulseAnimation(GameObject targetObject)
    {
        if (targetObject == null) yield break;

        // Only animate buttons, not panels or other UI elements
        Button targetButton = targetObject.GetComponent<Button>();
        if (targetButton == null) yield break;

        Transform targetTransform = targetObject.transform;
        Vector3 originalScale = targetTransform.localScale;

        while (tutorialActive && targetObject != null && targetObject.activeInHierarchy)
        {
            // Scale up
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                if (targetObject == null) yield break;
                elapsed += Time.deltaTime;
                float progress = elapsed / (pulseDuration / 2f);
                float scale = Mathf.Lerp(1f, pulseScaleAmount, progress);
                targetTransform.localScale = originalScale * scale;
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < pulseDuration / 2f)
            {
                if (targetObject == null) yield break;
                elapsed += Time.deltaTime;
                float progress = elapsed / (pulseDuration / 2f);
                float scale = Mathf.Lerp(pulseScaleAmount, 1f, progress);
                targetTransform.localScale = originalScale * scale;
                yield return null;
            }
        }

        // Restore original scale when animation stops
        if (targetObject != null)
        {
            targetTransform.localScale = originalScale;
        }
    }

    void UpdateButtonInteractability(TutorialStep currentStep)
    {
        // Keep buttons interactable during tutorial, but OCRManager will block functionality
        // This allows the visual feedback while preventing actual OCR processing

        if (captureButton != null)
            captureButton.interactable = true; // Always true, let OCRManager block functionality

        if (galleryButton != null)
            galleryButton.interactable = true; // Always true, let OCRManager block functionality

        if (instructionsButton != null)
            instructionsButton.interactable = (currentStep.interactionTarget == "Instructions");

        if (exitScanButton != null)
            exitScanButton.interactable = (currentStep.interactionTarget == "CloseResults");
    }

    void OnNextClicked()
    {
        // Only advance if this is an informational step (Next button visible)
        if (btnInsNext.gameObject.activeSelf)
        {
            AdvanceStep();
        }
    }

    void AdvanceStep()
    {
        // Stop any running pulse animation
        if (currentPulseCoroutine != null)
        {
            StopCoroutine(currentPulseCoroutine);
            currentPulseCoroutine = null;
        }

        // restore any reparented element before going to next step
        RestoreAllSavedElements();

        currentIndex++;
        if (currentIndex >= tutorialSteps.Count) { EndTutorial(); return; }
        ShowCurrentStep();
    }

    IEnumerator WaitForUserAction(string target)
    {
        bool acted = false;
        bool skipped = false;

        // FIX: Add skip listener alongside the action listener
        UnityEngine.Events.UnityAction skipAction = () => skipped = true;
        btnInsSkip.onClick.AddListener(skipAction);
        temporaryUnsubs.Add(() => { try { btnInsSkip.onClick.RemoveListener(skipAction); } catch { } });

        void AddTempListener(Button btn)
        {
            UnityEngine.Events.UnityAction action = () => acted = true;
            btn.onClick.AddListener(action);
            temporaryUnsubs.Add(() => { try { btn.onClick.RemoveListener(action); } catch { } });
        }

        switch (target)
        {
            case "Capture":
                if (captureButton != null) AddTempListener(captureButton);
                break;
            case "Gallery":
                if (galleryButton != null) AddTempListener(galleryButton);
                break;
            case "Instructions":
                if (instructionsButton != null) AddTempListener(instructionsButton);
                break;
            case "CloseResults":
                if (exitScanButton != null) AddTempListener(exitScanButton);
                break;
            default:
                AddTempListener(btnInsNext);
                break;
        }

        // FIX: Wait for either the target action OR skip
        yield return new WaitUntil(() => acted || skipped);

        foreach (var unsub in temporaryUnsubs) unsub.Invoke();
        temporaryUnsubs.Clear();

        RestoreAllSavedElements();
        panelBackground.raycastTarget = true;

        // If skipped, end tutorial immediately
        if (skipped)
        {
            EndTutorialImmediate();
            yield break;
        }

        // SMALL DELAY before advancing
        yield return new WaitForSeconds(0.1f);

        // ADVANCE THE STEP
        currentIndex++;
        if (currentIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            yield break;
        }

        ShowCurrentStep();
    }

    IEnumerator WaitForTutorialAction(string target)
    {
        bool acted = false;
        bool skipped = false;
        Coroutine simulationCoroutine = null;

        // FIX: Add skip listener
        UnityEngine.Events.UnityAction skipAction = () => skipped = true;
        btnInsSkip.onClick.AddListener(skipAction);
        temporaryUnsubs.Add(() => { try { btnInsSkip.onClick.RemoveListener(skipAction); } catch { } });

        void AddTempListener(Button btn, Action customAction = null)
        {
            UnityEngine.Events.UnityAction action = () =>
            {
                acted = true;
                // Store the coroutine if we start one
                if (customAction != null)
                {
                    simulationCoroutine = StartCoroutine(RunCustomAction(customAction));
                }
            };
            btn.onClick.AddListener(action);
            temporaryUnsubs.Add(() => { try { btn.onClick.RemoveListener(action); } catch { } });
        }

        switch (target)
        {
            case "Capture":
                if (captureButton != null)
                {
                    AddTempListener(captureButton, () => StartCoroutine(SimulateCaptureResultWithCompletion()));
                }
                break;
            case "Gallery":
                if (galleryButton != null)
                {
                    AddTempListener(galleryButton, SimulateGalleryPick);
                }
                break;
            default:
                if (captureButton != null) AddTempListener(captureButton);
                break;
        }

        // FIX: Wait for either action OR skip
        yield return new WaitUntil(() => acted || skipped);

        // If we started a simulation coroutine, wait for it to complete (unless skipped)
        if (simulationCoroutine != null && !skipped)
        {
            yield return simulationCoroutine;
        }

        foreach (var unsub in temporaryUnsubs) unsub.Invoke();
        temporaryUnsubs.Clear();

        RestoreAllSavedElements();

        // If skipped, end tutorial immediately
        if (skipped)
        {
            EndTutorialImmediate();
            yield break;
        }

        // SMALL DELAY before advancing to ensure UI updates properly
        yield return new WaitForSeconds(0.1f);

        // ADVANCE THE STEP
        currentIndex++;
        if (currentIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            yield break;
        }

        ShowCurrentStep();
    }

    // Helper coroutine to run custom actions
    IEnumerator RunCustomAction(Action action)
    {
        action?.Invoke();
        yield return null;
    }

    // Replace SimulateCaptureResult with this version that returns a coroutine
    IEnumerator SimulateCaptureResultWithCompletion()
    {
        // Show results panel with smooth transition during tutorial
        if (resultsPanel != null)
        {
            yield return StartCoroutine(ShowResultsPanelWithTransition());
        }

        // Wait a bit for the user to see the results
        yield return new WaitForSeconds(0.5f);
    }

    void SimulateCaptureResult()
    {
        StartCoroutine(SimulateCaptureResultWithCompletion());
    }

    void SimulateGalleryPick()
    {
        // Gallery button functionality is completely disabled during tutorial
        // No results panel will open, no gallery will be accessed
        Debug.Log("Tutorial: Gallery button clicked - functionality disabled for tutorial");
        StartCoroutine(FlashButton(galleryButton));
    }

    IEnumerator FlashButton(Button b)
    {
        if (b == null) yield break;
        Image img = b.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        img.color = Color.gray;
        yield return new WaitForSeconds(0.2f);
        img.color = original;
    }

    IEnumerator ShowResultsPanelWithTransition()
    {
        // Ensure panel background stays active during results display
        panelBackground.gameObject.SetActive(true);
        panelBackground.color = new Color(panelBackground.color.r, panelBackground.color.g, panelBackground.color.b, 0.9f);

        // Ensure results panel is active but start transparent
        resultsPanel.SetActive(true);

        // Get CanvasGroup for smooth alpha transition, or create one if needed
        CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = resultsPanel.AddComponent<CanvasGroup>();

        // Start transparent
        canvasGroup.alpha = 0f;

        // Fade in smoothly
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        Debug.Log("Tutorial: Simulating capture result - showing results panel with transition");
    }

    // Save original parent + index and re-parent element under TutorialPanel directly after PanelBackground
    void BringElementAboveBackground(GameObject go)
    {
        if (go == resultsPanel) return;

        Transform tr = go.transform;
        if (savedTransforms.ContainsKey(tr)) return; // already saved

        // Save original
        savedTransforms[tr] = new SavedTransformInfo
        {
            parent = tr.parent,
            siblingIndex = tr.GetSiblingIndex()
        };

        // Reparent to tutorialPanel
        tr.SetParent(tutorialPanel.transform, false);

        // Find PanelBackground index
        int panelBgIndex = panelBackground.transform.GetSiblingIndex();

        // Place element right after PanelBackground
        tr.SetSiblingIndex(panelBgIndex + 1);

        // FIX: Ensure Skip button is ALWAYS the topmost element
        btnInsSkip.transform.SetAsLastSibling();
        btnInsNext.transform.SetAsLastSibling();

        // Then position text elements
        titleText.transform.SetAsLastSibling();
        descriptionText.transform.SetAsLastSibling();

        // FIX: One more time to be sure Skip is on top
        btnInsSkip.transform.SetAsLastSibling();
    }

    // Restore all saved elements to their original parents and sibling positions
    void RestoreAllSavedElements()
    {
        foreach (var kv in new Dictionary<Transform, SavedTransformInfo>(savedTransforms))
        {
            Transform tr = kv.Key;
            var info = kv.Value;
            if (tr == null) continue;

            try
            {
                tr.SetParent(info.parent, false);
                int clampedIndex = Mathf.Clamp(info.siblingIndex, 0, info.parent.childCount);
                tr.SetSiblingIndex(clampedIndex);

                // Restore original scale when returning to original parent
                tr.localScale = Vector3.one;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Restore failed: " + e.Message);
            }
        }
        savedTransforms.Clear();

        // Reset panel background to catch rays again
        if (panelBackground != null) panelBackground.raycastTarget = true;
    }

    // End tutorial normally
    void EndTutorial()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // ADD THIS: Clear tutorial flag in OCRManager
        OCRManager ocrManager = FindFirstObjectByType<OCRManager>();
        if (ocrManager != null)
        {
            ocrManager.IsTutorialActive = false;
        }

        StartCoroutine(FadeOverlay(0f, () =>
        {
            tutorialPanel.SetActive(false);
            panelBackground.gameObject.SetActive(false);
            if (cameraPreviewCover != null) cameraPreviewCover.gameObject.SetActive(false);
            tutorialActive = false;

            if (currentPulseCoroutine != null)
            {
                StopCoroutine(currentPulseCoroutine);
                currentPulseCoroutine = null;
            }

            RestoreButtonInteractability();
            RestoreAllSavedElements();

            if (resultsPanel != null && resultsPanel.activeSelf)
            {
                StartCoroutine(HideResultsPanelWithTransition());
            }
        }));
    }

    void RestoreButtonInteractability()
    {
        if (captureButton != null) captureButton.interactable = originalCaptureInteractable;
        if (galleryButton != null) galleryButton.interactable = originalGalleryInteractable;
        if (instructionsButton != null) instructionsButton.interactable = originalInstructionsInteractable;
        if (exitScanButton != null) exitScanButton.interactable = true;
    }

    IEnumerator HideResultsPanelWithTransition()
    {
        CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            resultsPanel.SetActive(false);
            yield break;
        }

        // Fade out smoothly
        float duration = 0.2f;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        resultsPanel.SetActive(false);
        canvasGroup.alpha = 1f; // Reset for next time
    }

    void EndTutorialImmediate()
    {
        foreach (var unsub in temporaryUnsubs) unsub.Invoke();
        temporaryUnsubs.Clear();
        RestoreAllSavedElements();

        // ADD THIS: Clear tutorial flag in OCRManager
        OCRManager ocrManager = FindFirstObjectByType<OCRManager>();
        if (ocrManager != null)
        {
            ocrManager.IsTutorialActive = false;
        }

        if (cameraPreviewCover != null) cameraPreviewCover.gameObject.SetActive(false);

        if (currentPulseCoroutine != null)
        {
            StopCoroutine(currentPulseCoroutine);
            currentPulseCoroutine = null;
        }

        RestoreButtonInteractability();

        if (resultsPanel != null)
        {
            CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            resultsPanel.SetActive(false);
        }

        tutorialPanel.SetActive(false);
        panelBackground.gameObject.SetActive(false);
        tutorialActive = false;

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
    }

    // Fade helper (from current alpha to targetAlpha)
    IEnumerator FadeOverlayCoroutine(float fromA, float toA, Action onComplete)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(fromA, toA, t / dur);
            if (panelBackground != null)
            {
                var c = panelBackground.color;
                panelBackground.color = new Color(c.r, c.g, c.b, a);
            }
            yield return null;
        }
        onComplete?.Invoke();
    }

    IEnumerator FadeOverlay(float fromA, float toA)
    {
        yield return StartCoroutine(FadeOverlayCoroutine(fromA, toA, null));
    }

    IEnumerator FadeOverlay(float toA, Action onComplete)
    {
        float fromA = panelBackground != null ? panelBackground.color.a : 0f;
        yield return StartCoroutine(FadeOverlayCoroutine(fromA, toA, onComplete));
    }
}
