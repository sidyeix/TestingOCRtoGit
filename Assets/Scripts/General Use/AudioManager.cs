using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clip")]
    [SerializeField] private AudioClip mainMenuBGM;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float volume = 1f; // Adjustable in Inspector

    private void Awake()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        musicSource.clip = mainMenuBGM;
        musicSource.volume = volume;
        musicSource.loop = true; // optional, keep music looping
    }

    private void Start()
    {
        musicSource.Play();
    }

    // Optional: Update volume at runtime if slider is used
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        musicSource.volume = volume;
    }
}
