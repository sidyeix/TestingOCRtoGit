using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class UXManager : MonoBehaviour
{
    [Header("References (existing UI)")]
    public BattleManager battleManager;         // assign your BattleManager instance
    public TMP_Text energyText;                 // same as BattleManager.energyText
    public TMP_Text playerHealthText;           // same as BM
    public TMP_Text npcHealthText;              // same as BM
    public TMP_Text phaseText;                  // same as BM
    public TMP_Text npcAssignedText;            // to detect 'Waiting...' and show thinking

    [Header("Pause & Settings UI")]
    public GameObject pausePanel;               // panel with Resume / Restart / Quit
    public Button pauseButton;                  // small top-right button
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle muteToggle;
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Audio")]
    public AudioSource backgroundMusicSource;   // wire to same source used by BM (if any)
    public AudioSource sfxSource;               // optional local sfx source for UI

    [Header("Energy UI")]
    public Transform energyOrbsParent;          // horizontal layout group for orbs
    public GameObject energyOrbPrefab;          // small circular orb prefab
    public int maxOrbs = 5;                     // matches BM.maxEnergy ideally

    [Header("Combat Feedback")]
    public GameObject damagePopupPrefab;        // floating number prefab (TextMeshPro)
    public GameObject blockEffectPrefab;        // small particle for block
    public GameObject critEffectPrefab;         // particle/flash for crit
    public Transform playerHitAnchor;           // world or UI anchor to spawn popups
    public Transform npcHitAnchor;

    [Header("Phase Banner")]
    public GameObject phaseBannerPrefab;        // slide/fade banner prefab
    public Transform uiRoot;

    [Header("Action Preview")]
    public GameObject previewPanel;             // small tooltip panel
    public TMP_Text previewText;

    [Header("Character Feedback")]
    public Image playerPortraitGlow;            // small glow image around player portrait in HUD
    public Image npcPortraitGlow;

    // internal
    int previousPlayerHP = -1;
    int previousNpcHP = -1;
    int previousEnergy = -1;
    CanvasGroup canvasGroup;

    // settings keys
    const string PREF_MUSIC = "NV_MusicVol";
    const string PREF_SFX = "NV_SFXVol";
    const string PREF_MUTE = "NV_Mute";

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        LoadSettings();

        // Ensure required refs
        if (battleManager == null)
            Debug.LogWarning("BattleUXManager: assign BattleManager reference.");

        // wire pause button
        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);

        // wire resume/restart/quit
        if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
        if (restartButton != null) restartButton.onClick.AddListener(RestartBattle);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(SetMute);

        // init UI
        if (energyOrbsParent != null && energyOrbPrefab != null)
            CreateEnergyOrbs();

        if (previewPanel != null) previewPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Start()
    {
        // parse initial health/energy values
        previousPlayerHP = ParseHealthText(playerHealthText);
        previousNpcHP = ParseHealthText(npcHealthText);
        previousEnergy = ParseEnergyText();

        // show initial phase banner
        if (phaseText != null)
            StartCoroutine(ShowPhaseBannerOnce(phaseText.text));
    }

    void Update()
    {
        PollEnergy();
        PollHealth();
        PollPhaseText();
        PollNpcThinking();
    }

    // ---------- Pause / Settings ----------
    void TogglePause()
    {
        bool currentlyPaused = Time.timeScale == 0f;
        if (!currentlyPaused)
        {
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            // optionally dim the UI
            canvasGroup.alpha = 0.9f;
        }
        else
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            canvasGroup.alpha = 1f;
        }
    }

    void RestartBattle()
    {
        // simple approach: reload scene
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void QuitToMenu()
    {
        Time.timeScale = 1f;
        // use your BM.returnSceneName or set directly
        if (battleManager != null && !string.IsNullOrEmpty(battleManager.returnSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(battleManager.returnSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void SetMusicVolume(float v)
    {
        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = v;
        PlayerPrefs.SetFloat(PREF_MUSIC, v);
    }

    void SetSfxVolume(float v)
    {
        if (sfxSource != null)
            sfxSource.volume = v;
        PlayerPrefs.SetFloat(PREF_SFX, v);
    }

    void SetMute(bool muted)
    {
        AudioListener.pause = muted;
        PlayerPrefs.SetInt(PREF_MUTE, muted ? 1 : 0);
    }

    void LoadSettings()
    {
        float m = PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        float s = PlayerPrefs.GetFloat(PREF_SFX, 1f);
        bool muted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;

        if (musicSlider != null) musicSlider.value = m;
        if (sfxSlider != null) sfxSlider.value = s;
        if (muteToggle != null) muteToggle.isOn = muted;

        if (backgroundMusicSource != null) backgroundMusicSource.volume = m;
        if (sfxSource != null) sfxSource.volume = s;
        AudioListener.pause = muted;
    }

    // ---------- Energy UI ----------
    void CreateEnergyOrbs()
    {
        // clear existing
        foreach (Transform c in energyOrbsParent) Destroy(c.gameObject);
        for (int i = 0; i < maxOrbs; i++)
        {
            var go = Instantiate(energyOrbPrefab, energyOrbsParent);
            go.name = $"Orb_{i+1}";
            // set default state
            var img = go.GetComponent<Image>();
            if (img != null) img.fillAmount = 0f;
        }
    }

    void PollEnergy()
    {
        int currentEnergy = ParseEnergyText();
        if (currentEnergy != previousEnergy)
        {
            UpdateEnergyOrbs(currentEnergy);
            previousEnergy = currentEnergy;

            // small character feedback when energy full
            if (currentEnergy >= maxOrbs)
            {
                StartCoroutine(GlowPortrait(playerPortraitGlow));
            }
            else
            {
                StopGlow(playerPortraitGlow);
            }
        }
    }

    int ParseEnergyText()
    {
        if (energyText == null) return 0;
        int outv = 0;
        int.TryParse(energyText.text, out outv);
        return outv;
    }

    void UpdateEnergyOrbs(int energy)
    {
        int idx = 0;
        foreach (Transform t in energyOrbsParent)
        {
            var img = t.GetComponent<Image>();
            if (img == null) { idx++; continue; }
            // full orb if idx < energy
            if (idx < energy) img.fillAmount = 1f;
            else img.fillAmount = 0f;
            // animate on change
            if (idx < energy) StartCoroutine(Pulse(img.rectTransform));
            idx++;
        }
    }

    IEnumerator Pulse(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector3 original = rt.localScale;
        Vector3 target = original * 1.12f;
        float t = 0f;
        float dur = 0.12f;
        while (t < dur)
        {
            rt.localScale = Vector3.Lerp(original, target, t / dur);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        t = 0;
        while (t < dur)
        {
            rt.localScale = Vector3.Lerp(target, original, t / dur);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localScale = original;
    }

    IEnumerator GlowPortrait(Image portrait)
    {
        if (portrait == null) yield break;
        portrait.enabled = true;
        float t = 0f;
        while (true)
        {
            portrait.color = Color.Lerp(new Color(1, 1, 1, 0.2f), new Color(1, 1, 1, 0.9f), Mathf.PingPong(Time.unscaledTime * 1.2f, 1f));
            yield return null;
        }
    }

    void StopGlow(Image portrait)
    {
        if (portrait == null) return;
        portrait.enabled = false;
    }

    // ---------- Damage & Health Polling ----------
    void PollHealth()
    {
        int curP = ParseHealthText(playerHealthText);
        int curN = ParseHealthText(npcHealthText);

        if (previousPlayerHP != -1 && curP != previousPlayerHP)
        {
            int delta = previousPlayerHP - curP;
            if (delta > 0)
            {
                SpawnDamagePopup(playerHitAnchor, delta, false);
            }
            else if (delta < 0)
            {
                // heal popup (optional)
                SpawnHealPopup(playerHitAnchor, -delta);
            }
        }

        if (previousNpcHP != -1 && curN != previousNpcHP)
        {
            int delta = previousNpcHP - curN;
            if (delta > 0)
            {
                SpawnDamagePopup(npcHitAnchor, delta, false);
            }
            else if (delta < 0)
            {
                SpawnHealPopup(npcHitAnchor, -delta);
            }
        }

        previousPlayerHP = curP;
        previousNpcHP = curN;
    }

    int ParseHealthText(TMP_Text hpText)
    {
        if (hpText == null) return -1;
        // expects format "X/Y"
        string s = hpText.text;
        var parts = s.Split('/');
        int v = -1;
        if (parts.Length > 0) int.TryParse(parts[0], out v);
        return v;
    }

    void SpawnDamagePopup(Transform anchor, int amount, bool isCrit)
    {
        if (damagePopupPrefab == null || anchor == null) return;
        var go = Instantiate(damagePopupPrefab, uiRoot);
        go.transform.position = Camera.main.WorldToScreenPoint(anchor.position);
        var t = go.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = $"-{amount}";
        // color/scale for crit
        if (isCrit && t != null) { t.fontSize += 6; /* tweak visuals */ }
        Destroy(go, 1.6f);
    }

    void SpawnHealPopup(Transform anchor, int amount)
    {
        if (damagePopupPrefab == null || anchor == null) return;
        var go = Instantiate(damagePopupPrefab, uiRoot);
        go.transform.position = Camera.main.WorldToScreenPoint(anchor.position);
        var t = go.GetComponentInChildren<TMP_Text>();
        if (t != null) { t.text = $"+{amount}"; t.color = Color.green; }
        Destroy(go, 1.6f);
    }

    // ---------- Phase / NPC Thinking ----------
    void PollPhaseText()
    {
        if (phaseText == null) return;
        // when phase text changes, show banner
        // Use text itself to decide
        // simple heuristic: show banner on text change (we keep last shown)
        // to avoid expensive string compare each frame, optional: cache lastPhase
        // (we'll just call ShowPhaseBanner if text contains "Assign" or "Cooldown" etc.)
    }

    string lastPhaseShown = "";

    void PollNpcThinking()
    {
        if (npcAssignedText == null) return;
        string s = npcAssignedText.text;
        // show a short thinking indicator when it reads "Waiting..."
        if (s.ToLower().Contains("waiting"))
        {
            // spawn a small bubble near NPC or overlay (optional)
            // we'll show a brief banner once
            if (lastPhaseShown != "npc_wait")
            {
                StartCoroutine(ShowTemporaryBanner("NPC is planning...", 0.8f));
                lastPhaseShown = "npc_wait";
            }
        }
    }

    IEnumerator ShowPhaseBannerOnce(string phase)
    {
        if (string.IsNullOrEmpty(phase) || phaseBannerPrefab == null) yield break;
        var go = Instantiate(phaseBannerPrefab, uiRoot);
        var txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = phase;
        Destroy(go, 1.4f);
        yield return null;
    }

    IEnumerator ShowTemporaryBanner(string text, float duration)
    {
        if (phaseBannerPrefab == null) yield break;
        var go = Instantiate(phaseBannerPrefab, uiRoot);
        var txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = text;
        yield return new WaitForSecondsRealtime(duration);
        Destroy(go);
    }

    // ---------- Action Preview ----------
    // Call these from Button EventTriggers (PointerEnter / PointerExit),
    // or hook them to UI Button OnPointerEnter via inspector
    public void ShowAttackPreview()
    {
        if (battleManager == null) return;
        // estimate using BM public properties
        int energy = ParseEnergyText();
        int attackEstimate = Mathf.Max(0, energy); // simple, or base on chosen assign
        // attempt a more realistic estimate: use BM.baseDamage & BM.defenseMitigationFactor
        int estimatedDamage = CalculateEstimatedDamage(attackEstimate, GetNpcDefendGuess());
        previewText.text = $"Attack Preview\nEst. {estimatedDamage} dmg";
        previewPanel.SetActive(true);
    }

    public void ShowDefendPreview()
    {
        previewText.text = "Defend Preview\nReduces incoming damage";
        previewPanel.SetActive(true);
    }

    public void HidePreview()
    {
        if (previewPanel != null) previewPanel.SetActive(false);
    }

    int GetNpcDefendGuess()
    {
        // look at npcAssignedText: if it contains "Defend: X" we parse it, else use 0
        if (npcAssignedText == null) return 0;
        string s = npcAssignedText.text;
        if (s.ToLower().Contains("defend"))
        {
            int idx = s.ToLower().IndexOf("defend");
            // naive parse - find digits after colon
            var digits = System.Text.RegularExpressions.Regex.Match(s.Substring(idx), @"\d+");
            if (digits.Success) return int.Parse(digits.Value);
        }
        return 0;
    }

    int CalculateEstimatedDamage(int attackPoints, int defendPoints)
    {
        if (battleManager == null) return attackPoints * 10;
        // replicate BM formula in simplified way:
        float effDefense = Mathf.Pow(defendPoints, battleManager.defenseMitigationFactor);
        int netAttack = Mathf.Max(0, attackPoints - Mathf.RoundToInt(effDefense));
        int damage = Mathf.Max(1, netAttack * battleManager.baseDamage);
        return damage;
    }

    // ---------- Utilities ----------
    IEnumerator FadeCanvas(float to, float duration)
    {
        float from = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
