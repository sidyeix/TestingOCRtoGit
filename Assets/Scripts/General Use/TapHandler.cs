using UnityEngine;
using UnityEngine.SceneManagement;

public class TapHandler : MonoBehaviour
{
    [SerializeField] public string nextSceneName = "Main Menu"; // scene to load on tap
    [SerializeField] private RectTransform[] ignoredUIElements;
    [SerializeField] private string activeSceneName; // scene where this TapHandler is active

    void Start()
    {
        // If not manually assigned, default to the scene this object is in
        if (string.IsNullOrEmpty(activeSceneName))
            activeSceneName = SceneManager.GetActiveScene().name;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (!enabled) return;

        // NEW: Check if tutorial is active
        if (IsTutorialActive())
        {
            return; // Don't process taps during tutorial
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsOverIgnoredElement(Input.mousePosition))
                SceneManager.LoadScene(nextSceneName);
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!IsOverIgnoredElement(Input.GetTouch(0).position))
                SceneManager.LoadScene(nextSceneName);
        }
    }

    // NEW: Check if tutorial is active
    private bool IsTutorialActive()
    {
        TutorialManager tutorialManager = FindFirstObjectByType<TutorialManager>();
        return tutorialManager != null && tutorialManager.IsTutorialActive();
    }

    private bool IsOverIgnoredElement(Vector2 screenPosition)
    {
        foreach (var rect in ignoredUIElements)
        {
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
                return true;
        }
        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only enable TapHandler in the scene it's intended for
        enabled = scene.name == activeSceneName;
    }
}