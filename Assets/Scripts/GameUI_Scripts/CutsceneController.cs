using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

[System.Serializable]
public class CutscenePanel
{
    public GameObject panel;       // Panel GameObject
    public TMP_Text dialogueText;  // Panel's TextMeshPro object
}

public class CutsceneController : MonoBehaviour
{
    [Header("Cutscene Panels")]
    public CutscenePanel[] cutscenePanels;

    [Header("UI Elements")]
    public Button exitButton;
    public string exitSceneName = "ScanScene";
    public string proceedToGameplay = "Battleplay";

    [Header("Text Settings")]
    [Range(0.01f, 0.2f)]
    public float textSpeed = 0.03f; // Typing speed

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string fullText = ""; // Stores full text of current panel

    void Start()
    {
        if (cutscenePanels.Length == 0) return;

        // Activate only the first panel
        for (int i = 0; i < cutscenePanels.Length; i++)
            cutscenePanels[i].panel.SetActive(i == 0);

        exitButton.onClick.AddListener(OnExitButtonClicked);

        ShowPanel(currentIndex);
    }

    void Update()
    {
        if (IsClickOrTouch() && !IsPointerOverUI())
        {
            if (isTyping)
            {
                CompleteText();
            }
            else
            {
                NextPanel();
            }
        }
    }

    private bool IsClickOrTouch()
    {
        return Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
        }
        else
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return true;
        }

        return false;
    }

    private void ShowPanel(int index)
    {
        if (index < 0 || index >= cutscenePanels.Length) return;

        // Activate only this panel
        for (int i = 0; i < cutscenePanels.Length; i++)
            cutscenePanels[i].panel.SetActive(i == index);

        TMP_Text textObject = cutscenePanels[index].dialogueText;
        if (textObject != null)
        {
            fullText = textObject.text; // Save full text
            textObject.text = "";        // Clear before typing

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(textObject, fullText));
        }
    }

    private IEnumerator TypeText(TMP_Text textObject, string message)
    {
        isTyping = true;

        foreach (char c in message)
        {
            textObject.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    private void CompleteText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        TMP_Text textObject = cutscenePanels[currentIndex].dialogueText;
        if (textObject != null)
            textObject.text = fullText; // Instantly show full text

        isTyping = false;
    }

    private void NextPanel()
    {
        // If last panel, go to exit scene
        if (currentIndex >= cutscenePanels.Length - 1)
        {
            SceneManager.LoadScene(proceedToGameplay);
            return;
        }

        currentIndex++;
        ShowPanel(currentIndex);
    }

    private void OnExitButtonClicked()
    {
        SceneManager.LoadScene(exitSceneName);
    }
}
