using UnityEngine;

// Pasang script ini di 1 GameObject kosong (misal "AudioManager") di scene pertama
// (misalnya MainMenuScene), karena dia DontDestroyOnLoad seperti EnemyManager/PartyManager.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;   // untuk suara pendek (attack, hit, dll)
    [SerializeField] private AudioSource musicSource; // untuk musik latar (opsional)

    [Header("SFX Settings")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] [Range(0f, 0.2f)] private float pitchVariationRange = 0.05f; // variasi nada biar tidak monoton

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Panggil dari mana saja: AudioManager.Instance.PlaySFX(someClip);
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = 1f + Random.Range(-pitchVariationRange, pitchVariationRange);
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = value;
    }

    public void SetMusicVolume(float value)
    {
        if (musicSource != null) musicSource.volume = value;
    }
}
