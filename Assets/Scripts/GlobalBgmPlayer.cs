using UnityEngine;

public class GlobalBgmPlayer : MonoBehaviour
{
    private static GlobalBgmPlayer instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float volume = 0.35f;
    public bool playOnAwake = true;
    public bool loop = true;
    [Range(0f, 1f)]
    public float spatialBlend = 0f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ApplyAudioSettings();
    }

    private void Start()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    private void OnValidate()
    {
        if (audioSource != null)
        {
            ApplyAudioSettings();
        }
    }

    public void Play()
    {
        if (audioSource == null || bgmClip == null)
        {
            return;
        }

        if (audioSource.clip != bgmClip)
        {
            audioSource.clip = bgmClip;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void ApplyAudioSettings()
    {
        audioSource.clip = bgmClip;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
        audioSource.dopplerLevel = 0f;
    }
}
