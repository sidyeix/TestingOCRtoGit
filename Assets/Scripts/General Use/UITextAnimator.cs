using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UITextAnimator : MonoBehaviour
{
    private TMP_Text tmpText;
    private Vector3 baseScale;
    private string originalText;

    [Header("Pulse Settings")]
    public bool usePulse = false;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;

    [Header("Wave Settings")]
    public bool useWave = false;
    public float waveSpeed = 2f;
    public float waveHeight = 3f;

    [Header("Color Flicker Settings")]
    public bool useFlicker = false;
    public Color flickerColor = Color.yellow;
    public float flickerSpeed = 2f;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        baseScale = transform.localScale;
        originalText = tmpText.text;
    }

    void Update()
    {
        if (usePulse) DoPulse();
        if (useWave) DoWave();
        if (useFlicker) DoFlicker();
    }

    void DoPulse()
    {
        float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }

    void DoWave()
    {
        tmpText.ForceMeshUpdate();
        var textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            var verts = textInfo.meshInfo[materialIndex].vertices;

            float offset = Mathf.Sin(Time.time * waveSpeed + i * 0.3f) * waveHeight;

            verts[vertexIndex + 0].y += offset;
            verts[vertexIndex + 1].y += offset;
            verts[vertexIndex + 2].y += offset;
            verts[vertexIndex + 3].y += offset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    void DoFlicker()
    {
        float t = (Mathf.Sin(Time.time * flickerSpeed) + 1) / 2f;
        tmpText.color = Color.Lerp(Color.white, flickerColor, t);
    }
}
