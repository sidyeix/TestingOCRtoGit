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
    public TMP_Text passButtonText;

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

    [Header("Audio Settings")]
    public AudioSource backgroundMusicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioClip damageTakenSound;
    public AudioClip damageBlockedSound;

    [Header("Button Animations")]
    public float buttonScaleMultiplier = 1.1f;
    public float buttonScaleDuration = 0.1f;

    [Header("Animation Timing (seconds)")]
    public float attackAnimDuration = 1.1f;     // approx duration of attack animation
    public float blockAnimDuration = 0.7f;      // approx duration of block animation
    public float hitAnimDuration = 0.6f;        // duration of hit reaction
    public float interActionDelay = 0.25f;      // small gap between actions

    [Header("Advanced Balance Systems")]
    public float defenseMitigationFactor = 0.6f;   // lower = stronger defense, affects diminishing returns
    public int playerSpeed = 5;                    // higher = faster
    public int npcSpeed = 4;
    public TMP_Text feedbackText;                  // assign in Canvas (for messages like "Press End Turn to proceed")
    private int consecutivePlayerAttacks = 0;      // used for weighted crit
    private int consecutiveNPCAttacks = 0;

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

    public string passLabel = "Pass";
    public string endTurnLabel = "End Turn";

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

        SetupAudio();
        
        resultPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmResult);

        UpdateUI();
        StartNewRound();
    }

    void SetupAudio()
    {
        if (backgroundMusicSource != null && backgroundMusic != null)
        {
            backgroundMusicSource.clip = backgroundMusic;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
        
        AddButtonClickSounds();
    }

    void AddButtonClickSounds()
    {
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

    IEnumerator AnimateButton(Button button)
    {
        if (button == null) yield break;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector3 originalScale = rectTransform.localScale;
        Vector3 targetScale = originalScale * buttonScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < buttonScaleDuration)
        {
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / buttonScaleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

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

        if (feedbackText != null)
        {
            if (currentEnergy > 0 && (playerAttack + playerDefend) < startingEnergy)
                feedbackText.text = "Tip: You can press 'End Turn' to proceed early.";
            else
                feedbackText.text = "";
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
        
        StartCoroutine(AnimateButton(attackButton));
        
        UpdateUI();
        CheckAutoProceed();
        UpdatePassButtonLabel();
    }

    void AssignDefend()
    {
        if (currentEnergy <= 0) return;

        playerDefend++;
        currentEnergy--;
        
        StartCoroutine(AnimateButton(defendButton));
        
        UpdateUI();
        CheckAutoProceed();
        UpdatePassButtonLabel();
    }

    void AssignPass()
    {
        if (!canAssign || isProcessingTurn) return;

        // If player already acted, treat as End Turn
        if (playerAttack > 0 || playerDefend > 0)
        {
            canAssign = false;
            if (currentTimerCoroutine != null) StopCoroutine(currentTimerCoroutine);
            StartCoroutine(ProceedAfterAutoAssign());
            feedbackText.text = "";
        }
        else
        {
            // True manual pass
            playerPassed = true;
            canAssign = false;
            if (currentTimerCoroutine != null) StopCoroutine(currentTimerCoroutine);
            StartCoroutine(ProceedAfterPass());
            if (feedbackText != null) feedbackText.text = "You passed your turn.";
        }

        UpdateUI();
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
        UpdatePassButtonLabel();
    }

    IEnumerator EvaluateTurn()
    {
        isProcessingTurn = true;
        phaseText.text = "Phase: Evaluating";

        // Decide NPC actions first
        GenerateNPCActions();

        // Update assigned UI
        playerAssignedText.text = playerPassed ? "Player Passed" : $"Attack: {playerAttack} | Defend: {playerDefend}";
        npcAssignedText.text = npcPassed ? "NPC Passed" : $"Attack: {npcAttack} | Defend: {npcDefend}";

        // Small anticipation delay
        yield return new WaitForSeconds(0.6f);

        if (!playerPassed && playerAttack > 0 && !npcPassed && npcAttack > 0)
        {
            if (playerSpeed >= npcSpeed)
            {
                yield return StartCoroutine(PerformAttack(player, npc, playerAttack, npcDefend));
                yield return new WaitForSeconds(interActionDelay);
                yield return StartCoroutine(PerformAttack(npc, player, npcAttack, playerDefend));
            }
            else
            {
                yield return StartCoroutine(PerformAttack(npc, player, npcAttack, playerDefend));
                yield return new WaitForSeconds(interActionDelay);
                yield return StartCoroutine(PerformAttack(player, npc, playerAttack, npcDefend));
            }
        }

        // CASE: Player attack, NPC defend -> simultaneous
        else if (!playerPassed && playerAttack > 0 && !npcPassed && npcDefend > 0)
        {
            yield return StartCoroutine(PerformSimultaneous(player, npc, playerAttack, npcDefend, true));
        }
        // CASE: NPC attack, Player defend -> simultaneous
        else if (!npcPassed && npcAttack > 0 && !playerPassed && playerDefend > 0)
        {
            yield return StartCoroutine(PerformSimultaneous(npc, player, npcAttack, playerDefend, false));
        }
        // CASE: Player defends & NPC defends -> both block pose
        else if (!playerPassed && playerDefend > 0 && !npcPassed && npcDefend > 0)
        {
            player.PlayAction("Block");
            npc.PlayAction("Block");
            yield return new WaitForSeconds(blockAnimDuration);
        }
        // CASE: Only player acts (npc passed)
        else if (!playerPassed && (playerAttack > 0 || playerDefend > 0) && npcPassed)
        {
            if (playerAttack > 0)
                yield return StartCoroutine(PerformAttack(player, npc, playerAttack, npcDefend));
            else
            {
                player.PlayAction("Block");
                yield return new WaitForSeconds(blockAnimDuration);
            }
        }
        // CASE: Only npc acts (player passed)
        else if (!npcPassed && (npcAttack > 0 || npcDefend > 0) && playerPassed)
        {
            if (npcAttack > 0)
                yield return StartCoroutine(PerformAttack(npc, player, npcAttack, playerDefend));
            else
            {
                npc.PlayAction("Block");
                yield return new WaitForSeconds(blockAnimDuration);
            }
        }
        // CASE: Neither acted / both passed - no animations; just continue

        // small settle delay
        yield return new WaitForSeconds(0.35f);

        UpdateHealthUI();
        isProcessingTurn = false;
    }

    IEnumerator PerformAttack(CharacterStatus attacker, CharacterStatus target, int attackValue, int targetDefense)
    {
        // Play attack animation
        attacker.PlayAction("Attack");

        // Wait roughly the attack animation length (tweak attackAnimDuration in inspector)
        yield return new WaitForSeconds(attackAnimDuration);

        // Calculate and apply damage
        int damage = CalculateDamage(attackValue, targetDefense, attacker.gameObject.name);

        if (damage > 0)
        {
            target.TakeDamage(damage);
            target.PlayAction("Hit");                    // play hit reaction on target
            PlaySFX(damageTakenSound);
        }
        else
        {
            // No damage dealt -> played blocked sound and block pose
            if (targetDefense > 0 && damageBlockedSound != null)
                PlaySFX(damageBlockedSound);

            target.PlayAction("Block");
        }

        // Wait a short time so reactions finish before next action
        yield return new WaitForSeconds(hitAnimDuration);
    }

    IEnumerator PerformSimultaneous(CharacterStatus attacker, CharacterStatus defender, int attackValue, int defendValue, bool attackerIsPlayer)
    {
        // Start both animations at roughly same time
        attacker.PlayAction("Attack");
        defender.PlayAction("Block");

        // Allow overlap (choose a value slightly shorter than full attack to show reaction)
        float overlapWait = Mathf.Min(attackAnimDuration, blockAnimDuration);
        yield return new WaitForSeconds(overlapWait);

        // Resolve damage from attacker to defender
        int damage = CalculateDamage(attackValue, defendValue, attacker.gameObject.name);

        if (damage > 0)
        {
            defender.TakeDamage(damage);
            defender.PlayAction("Hit");
            PlaySFX(damageTakenSound);

        }
        else
        {
            if (defendValue > 0 && damageBlockedSound != null)
                PlaySFX(damageBlockedSound);
            // defender already played Block above
        }

        // Allow defender hit/block animation to finish
        yield return new WaitForSeconds(hitAnimDuration);
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

            if (playerDamage > 0)
            {
                npc.PlayAction("Hit");
            }
            
            if (playerDamage == 0 && npcDefend > 0 && damageBlockedSound != null)
            {
                PlaySFX(damageBlockedSound);
            }
        }

        if (!npcPassed && npcAttack > 0)
        {
            npcDamage = CalculateDamage(npcAttack, playerDefend, "NPC");
            
            if (npcDamage > 0)
            {
                player.TakeDamage(npcDamage);
                Debug.Log($"NPC deals {npcDamage} damage to Player (NPC Attack: {npcAttack}, Player Defend: {playerDefend})");

                player.PlayAction("Hit");
                if (damageTakenSound != null)
                {
                    PlaySFX(damageTakenSound);
                }
            }
            else if (playerDefend > 0 && damageBlockedSound != null)
            {
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
        if (attack <= 0) return 0;

        // --- Speed-based priority logic ---
        bool isPlayerAttacker = source.Contains("Player");
        bool attackerFirst = isPlayerAttacker ? playerSpeed >= npcSpeed : npcSpeed >= playerSpeed;

        // --- Defense scaling with diminishing returns ---
        float effectiveDefense = Mathf.Pow(defend, defenseMitigationFactor);
        int netAttack = Mathf.Max(0, attack - Mathf.RoundToInt(effectiveDefense));
        int damage = Mathf.Max(1, netAttack * baseDamage); // at least 1 dmg if attack > 0

        // --- Weighted critical chance (sigmoid growth) ---
        int consecutive = isPlayerAttacker ? consecutivePlayerAttacks : consecutiveNPCAttacks;
        float adjustedCritChance = 1f / (1f + Mathf.Exp(-((attack - 2f) + (consecutive * 0.5f)))); // sigmoid curve
        adjustedCritChance *= criticalChance; // scale with base chance

        if (Random.value < adjustedCritChance)
        {
            float actualMultiplier = criticalMultiplier + Random.Range(0f, 0.3f);
            damage = Mathf.RoundToInt(damage * actualMultiplier);
            Debug.Log($"{source} scored a CRITICAL! {damage} dmg (x{actualMultiplier:F1})");
        }

        // --- Track consecutive attacks ---
        if (isPlayerAttacker)
        {
            consecutivePlayerAttacks++;
            consecutiveNPCAttacks = 0;
        }
        else
        {
            consecutiveNPCAttacks++;
            consecutivePlayerAttacks = 0;
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
        
        // --- PLAYER ENERGY RESET (performance-scaled) ---
        int usedEnergy = playerAttack + playerDefend;
        int bonusEnergy = (usedEnergy < startingEnergy) ? 1 : 0; // reward if not all used
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + 1 + bonusEnergy);

        // --- NPC ENERGY RESET (fair scaling) ---
        int npcUsed = npcAttack + npcDefend;
        int npcBonus = npcPassed ? 0 : (npcUsed < startingEnergy ? 1 : 0); // if NPC keeps passing, it gains less
        npcEnergy = Mathf.Min(maxEnergy, npcEnergy + 1 + npcBonus);

        playerAttack = 0;
        playerDefend = 0;
        playerPassed = false;
        npcAttack = 0;
        npcDefend = 0;
        npcPassed = false;

        if (feedbackText != null)
        feedbackText.text = "";

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
        UpdatePassButtonLabel();
    }
    
        void UpdatePassButtonLabel()
    {
        if (passButtonText == null) return;

        // If player has assigned attack or defense points, change to "End Turn"
        if (playerAttack > 0 || playerDefend > 0)
        {
            passButtonText.text = endTurnLabel;
        }
        else
        {
            passButtonText.text = passLabel;
        }
    }


    void UpdateHealthUI()
    {
        if (playerHealthText != null)
            playerHealthText.text = $"{player.currentHealth}/{player.maxHealth}";
        
        if (npcHealthText != null)
            npcHealthText.text = $"{npc.currentHealth}/{npc.maxHealth}";
    }

    void ShowResultPanel(string result)
    {
        resultPanel.SetActive(true);
        resultText.text = result;

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

    void OnDestroy()
    {
        if (currentTimerCoroutine != null)
        {
            StopCoroutine(currentTimerCoroutine);
        }
    }
}