using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Characters")]
    public CharacterStatus player;
    public CharacterStatus npc;

    [Header("UI Buttons")]
    public Button attackButton;
    public Button defendButton;
    public Button passButton;
    public Button resetButton;

    [Header("UI Text")]
    public TMP_Text energyText;
    public TMP_Text roundText;
    public TMP_Text phaseText;
    public TMP_Text playerAssignedText;
    public TMP_Text npcAssignedText;
    public TMP_Text timerText;
    public TMP_Text cooldownText;
    public TMP_Text playerHealthText;
    public TMP_Text npcHealthText;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button confirmButton;
    public string returnSceneName = "MainMenu";

    [Header("Game Balance")]
    public int startingEnergy = 2;
    public int maxEnergy = 5;
    public float assignTime = 10f;
    public float cooldownTime = 3f;
    public int baseDamage = 10;
    [Range(0f, 1f)] public float criticalChance = 0.3f;
    public float criticalMultiplier = 1.5f;
    public int attackThresholdForCritical = 3;

    [Header("Vibration Settings")]
    public bool vibrationsEnabled = true;
    public float lightVibrationDuration = 0.1f;
    public float mediumVibrationDuration = 0.2f;
    public float heavyVibrationDuration = 0.3f;

    [Header("Audio Settings")]
    public AudioSource backgroundMusicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioClip damageTakenSound;
    public AudioClip damageBlockedSound;

    [Header("Screen Shake Settings")]
    public bool screenShakeEnabled = true;
    public float shakeDuration = 0.3f;
    public float shakeIntensity = 0.2f;

    [Header("Button Animations")]
    public float buttonScaleMultiplier = 1.1f;
    public float buttonScaleDuration = 0.1f;

    private int currentEnergy;
    private int npcEnergy;
    private int playerAttack = 0;
    private int playerDefend = 0;
    private bool playerPassed = false;

    private int npcAttack = 0;
    private int npcDefend = 0;
    private bool npcPassed = false;

    private int round = 1;
    private bool canAssign = true;
    private Coroutine currentTimerCoroutine;
    private bool isProcessingTurn = false;

    void Start()
    {
        ValidateReferences();
        InitializeGame();
    }

    void ValidateReferences()
    {
        if (player == null || npc == null)
        {
            Debug.LogError("Player or NPC CharacterStatus references not set in BattleManager!");
            return;
        }

        if (attackButton == null || defendButton == null || passButton == null || resetButton == null)
        {
            Debug.LogError("One or more UI buttons not assigned in BattleManager!");
        }
    }

    void InitializeGame()
    {
        currentEnergy = startingEnergy;
        npcEnergy = startingEnergy;
        
        attackButton.onClick.AddListener(() => OnAssignButton("Attack"));
        defendButton.onClick.AddListener(() => OnAssignButton("Defend"));
        passButton.onClick.AddListener(() => OnAssignButton("Pass"));
        resetButton.onClick.AddListener(ResetAssignments);

        // Setup audio
        SetupAudio();
        
        resultPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmResult);

        UpdateUI();
        StartNewRound();
    }

    void SetupAudio()
    {
        // Setup background music
        if (backgroundMusicSource != null && backgroundMusic != null)
        {
            backgroundMusicSource.clip = backgroundMusic;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
        
        // Add button click sounds to all battle buttons
        AddButtonClickSounds();
    }

    void AddButtonClickSounds()
    {
        // Only add to our main battle buttons
        Button[] battleButtons = new Button[] { attackButton, defendButton, passButton, resetButton, confirmButton };
        
        foreach (Button button in battleButtons)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => 
                {
                    PlaySFX(buttonClickSound);
                });
            }
        }
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    IEnumerator ScreenShake()
    {
        if (!screenShakeEnabled) yield break;
        
        // Get the camera more reliably
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // Fallback: find any camera in the scene
            mainCamera = FindAnyObjectByType<Camera>();
            if (mainCamera == null) yield break;
        }
        
        Transform cameraTransform = mainCamera.transform;
        Vector3 originalPosition = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Use Perlin noise for smoother, more natural shake
            float x = Mathf.PerlinNoise(Time.time * 10f, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, Time.time * 10f) * 2f - 1f;
            
            x *= shakeIntensity;
            y *= shakeIntensity;
            
            cameraTransform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Smoothly return to original position
        float returnTime = 0f;
        Vector3 currentPosition = cameraTransform.localPosition;
        while (returnTime < 0.1f)
        {
            cameraTransform.localPosition = Vector3.Lerp(currentPosition, originalPosition, returnTime / 0.1f);
            returnTime += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPosition;
    }

    IEnumerator AnimateButton(Button button)
    {
        if (button == null) yield break;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector3 originalScale = rectTransform.localScale;
        Vector3 targetScale = originalScale * buttonScaleMultiplier;

        // Scale up
        float elapsed = 0f;
        while (elapsed < buttonScaleDuration)
        {
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / buttonScaleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < buttonScaleDuration)
        {
            rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / buttonScaleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    void StartNewRound()
    {
        if (isProcessingTurn) return;

        roundText.text = $"Round: {round}";
        currentTimerCoroutine = StartCoroutine(RoundTimer());
    }

    IEnumerator RoundTimer()
    {
        canAssign = true;
        playerAttack = 0;
        playerDefend = 0;
        playerPassed = false;
        
        float timeLeft = assignTime;
        phaseText.text = "Phase: Assign Energy";

        attackButton.interactable = true;
        defendButton.interactable = true;
        passButton.interactable = true;
        resetButton.interactable = true;

        playerAssignedText.text = "Assigning...";
        npcAssignedText.text = "Waiting...";

        while (timeLeft > 0 && canAssign)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(timeLeft)}";
            yield return null;
            timeLeft -= Time.deltaTime;
        }

        timerText.text = "Time: 0";

        attackButton.interactable = false;
        defendButton.interactable = false;
        passButton.interactable = false;
        resetButton.interactable = false;
        canAssign = false;

        yield return StartCoroutine(EvaluateTurn());

        if (player.IsDead() || npc.IsDead())
        {
            ShowResultPanel(player.IsDead() ? "Defeat" : "Victory");
        }
        else
        {
            yield return StartCoroutine(CooldownPhase());
        }
    }

    void OnAssignButton(string type)
    {
        if (!canAssign || isProcessingTurn) return;

        switch (type)
        {
            case "Attack":
                AssignAttack();
                break;
            case "Defend":
                AssignDefend();
                break;
            case "Pass":
                AssignPass();
                break;
        }
    }

    void AssignAttack()
    {
        if (currentEnergy <= 0) return;

        playerAttack++;
        currentEnergy--;
        
        // Button animation
        StartCoroutine(AnimateButton(attackButton));
        
        UpdateUI();
        CheckAutoProceed();
    }

    void AssignDefend()
    {
        if (currentEnergy <= 0) return;

        playerDefend++;
        currentEnergy--;
        
        // Button animation
        StartCoroutine(AnimateButton(defendButton));
        
        UpdateUI();
        CheckAutoProceed();
    }

    void AssignPass()
    {
        playerPassed = true;
        canAssign = false;
        
        if (currentTimerCoroutine != null)
        {
            StopCoroutine(currentTimerCoroutine);
        }
        
        UpdateUI();
        StartCoroutine(ProceedAfterPass());
    }

    IEnumerator ProceedAfterPass()
    {
        yield return new WaitForSeconds(0.5f);
        
        attackButton.interactable = false;
        defendButton.interactable = false;
        passButton.interactable = false;
        resetButton.interactable = false;

        yield return StartCoroutine(EvaluateTurn());

        if (player.IsDead() || npc.IsDead())
        {
            ShowResultPanel(player.IsDead() ? "Defeat" : "Victory");
        }
        else
        {
            yield return StartCoroutine(CooldownPhase());
        }
    }

    void CheckAutoProceed()
    {
        if (currentEnergy <= 0)
        {
            canAssign = false;
            if (currentTimerCoroutine != null)
            {
                StopCoroutine(currentTimerCoroutine);
            }
            StartCoroutine(ProceedAfterAutoAssign());
        }
    }

    IEnumerator ProceedAfterAutoAssign()
    {
        yield return new WaitForSeconds(0.5f);
        
        attackButton.interactable = false;
        defendButton.interactable = false;
        passButton.interactable = false;
        resetButton.interactable = false;

        yield return StartCoroutine(EvaluateTurn());

        if (player.IsDead() || npc.IsDead())
        {
            ShowResultPanel(player.IsDead() ? "Defeat" : "Victory");
        }
        else
        {
            yield return StartCoroutine(CooldownPhase());
        }
    }

    void ResetAssignments()
    {
        if (!canAssign || isProcessingTurn) return;

        currentEnergy += playerAttack + playerDefend;
        playerAttack = 0;
        playerDefend = 0;
        playerPassed = false;
        
        UpdateUI();
    }

    IEnumerator EvaluateTurn()
    {
        isProcessingTurn = true;
        phaseText.text = "Phase: Evaluating";

        GenerateNPCActions();

        playerAssignedText.text = playerPassed ? "Player Passed" : $"Attack: {playerAttack} | Defend: {playerDefend}";
        npcAssignedText.text = npcPassed ? "NPC Passed" : $"Attack: {npcAttack} | Defend: {npcDefend}";

        yield return new WaitForSeconds(1f);

        CalculateAndApplyDamage();

        yield return new WaitForSeconds(1f);

        isProcessingTurn = false;
    }

    void GenerateNPCActions()
    {
        npcAttack = 0;
        npcDefend = 0;
        npcPassed = false;

        int remainingEnergy = npcEnergy;
        bool shouldPass = ShouldNPCPass(remainingEnergy);
        
        if (shouldPass)
        {
            npcPassed = true;
            return;
        }

        while (remainingEnergy > 0)
        {
            int choice = GetStrategicChoice(remainingEnergy);
            
            if (choice == 0)
            {
                npcAttack++;
                remainingEnergy--;
            }
            else if (choice == 1)
            {
                npcDefend++;
                remainingEnergy--;
            }
            else
            {
                break;
            }
        }

        int energyUsed = npcAttack + npcDefend;
        npcEnergy -= energyUsed;
    }

    bool ShouldNPCPass(int remainingEnergy)
    {
        float passChance = 0.1f;
        float healthRatio = (float)npc.currentHealth / npc.maxHealth;
        if (healthRatio < 0.2f) passChance += 0.3f;
        else if (healthRatio < 0.4f) passChance += 0.1f;
        if (playerAttack >= 4) passChance += 0.1f;
        if (remainingEnergy == 1 && round > 5) passChance += 0.1f;
        passChance = Mathf.Min(passChance, 0.4f);
        return Random.value < passChance;
    }

    int GetStrategicChoice(int remainingEnergy)
    {
        float healthRatio = (float)npc.currentHealth / npc.maxHealth;
        float playerHealthRatio = (float)player.currentHealth / player.maxHealth;

        if (healthRatio > playerHealthRatio + 0.3f) return 0;
        if (healthRatio < playerHealthRatio - 0.3f && remainingEnergy > 1) return 1;
        if (playerAttack >= 3 && healthRatio < 0.7f && remainingEnergy > 1) 
            return Random.value < 0.7f ? 1 : 0;
        return Random.value < 0.7f ? 0 : 1;
    }

    void CalculateAndApplyDamage()
    {
        int playerDamage = 0;
        int npcDamage = 0;

        if (!playerPassed && playerAttack > 0)
        {
            playerDamage = CalculateDamage(playerAttack, npcDefend, "Player");
            npc.TakeDamage(playerDamage);
            Debug.Log($"Player deals {playerDamage} damage to NPC (Player Attack: {playerAttack}, NPC Defend: {npcDefend})");
            
            // Play damage blocked sound if NPC defended successfully
            if (playerDamage == 0 && npcDefend > 0 && damageBlockedSound != null)
            {
                PlaySFX(damageBlockedSound);
            }
            
            // Vibrate when player deals damage
            if (playerDamage > 0)
            {
                Vibrate();
            }
        }

        if (!npcPassed && npcAttack > 0)
        {
            npcDamage = CalculateDamage(npcAttack, playerDefend, "NPC");
            
            // Play damage taken or blocked sound
            if (npcDamage > 0)
            {
                player.TakeDamage(npcDamage);
                Debug.Log($"NPC deals {npcDamage} damage to Player (NPC Attack: {npcAttack}, Player Defend: {playerDefend})");
                
                // Debug log to confirm screen shake is called
                Debug.Log("Screen shake triggered! Damage: " + npcDamage);
                
                // Play damage taken sound
                if (damageTakenSound != null)
                {
                    PlaySFX(damageTakenSound);
                }
                
                // Screen shake when player takes damage
                StartCoroutine(ScreenShake());
                
                // Vibrate when player takes damage
                Vibrate();
            }
            else if (playerDefend > 0 && damageBlockedSound != null)
            {
                // Play damage blocked sound
                PlaySFX(damageBlockedSound);
                Debug.Log($"Player blocked NPC's attack! (NPC Attack: {npcAttack}, Player Defend: {playerDefend})");
            }
        }

        if (playerDamage == 0 && npcDamage == 0)
        {
            Debug.Log("No damage dealt this round.");
        }

        UpdateHealthUI();
    }

    int CalculateDamage(int attack, int defend, string source)
    {
        int netAttack = Mathf.Max(attack - defend, 0);
        int damage = netAttack * baseDamage;

        if (attack >= 3)
        {
            float multiplierChance = criticalChance * (attack / 3f);
            multiplierChance = Mathf.Min(multiplierChance, 0.8f);

            if (Random.value < multiplierChance)
            {
                float actualMultiplier = criticalMultiplier + (Random.value * 0.5f);
                damage = Mathf.RoundToInt(damage * actualMultiplier);
                Debug.Log($"{source} triggered multiplier! {damage} damage (x{actualMultiplier:F1})");
            }
        }

        return damage;
    }

    IEnumerator CooldownPhase()
    {
        phaseText.text = "Phase: Cooldown";
        float timeLeft = cooldownTime;

        while (timeLeft > 0)
        {
            cooldownText.text = $"Next Round: {Mathf.CeilToInt(timeLeft)}s";
            yield return null;
            timeLeft -= Time.deltaTime;
        }

        cooldownText.text = "";

        round++;
        
        if (currentEnergy <= 0)
        {
            currentEnergy = 1;
        }
        else
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + 1);
        }

        if (npcEnergy <= 0)
        {
            npcEnergy = 1;
        }
        else
        {
            npcEnergy = Mathf.Min(maxEnergy, npcEnergy + 1);
        }

        playerAttack = 0;
        playerDefend = 0;
        playerPassed = false;
        npcAttack = 0;
        npcDefend = 0;
        npcPassed = false;

        UpdateUI();
        StartNewRound();
    }

    void UpdateUI()
    {
        currentEnergy = Mathf.Max(0, currentEnergy);
        playerAttack = Mathf.Max(0, playerAttack);
        playerDefend = Mathf.Max(0, playerDefend);

        energyText.text = $"{currentEnergy}";
        roundText.text = $"Round: {round}";
        
        attackButton.interactable = canAssign && currentEnergy > 0 && !isProcessingTurn;
        defendButton.interactable = canAssign && currentEnergy > 0 && !isProcessingTurn;
        passButton.interactable = canAssign && !isProcessingTurn;
        
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (playerHealthText != null)
            playerHealthText.text = $"Player: {player.currentHealth}/{player.maxHealth}";
        
        if (npcHealthText != null)
            npcHealthText.text = $"NPC: {npc.currentHealth}/{npc.maxHealth}";
    }

    void ShowResultPanel(string result)
    {
        resultPanel.SetActive(true);
        resultText.text = result;

        // Vibrate on game end
        Vibrate();

        attackButton.interactable = false;
        defendButton.interactable = false;
        passButton.interactable = false;
        resetButton.interactable = false;

        if (currentTimerCoroutine != null)
        {
            StopCoroutine(currentTimerCoroutine);
        }
    }

    void OnConfirmResult()
    {
        if (!string.IsNullOrEmpty(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
        else
        {
            Debug.LogWarning("Return scene name not set!");
        }
    }

    void Vibrate()
    {
        if (!vibrationsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        UnityEngine.iOS.Device.Vibrate();
#endif
    }

    void OnDestroy()
    {
        if (currentTimerCoroutine != null)
        {
            StopCoroutine(currentTimerCoroutine);
        }
    }
}