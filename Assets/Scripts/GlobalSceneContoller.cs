using System.Collections;
using System.Collections.Generic;
using Unity.XR.Oculus;
using UnityEngine;
using UnityEngine.SceneManagement;

using Debug = UnityEngine.Debug;

public class GlobalSceneController : MonoBehaviour
{
    public static GlobalSceneController instance;

    [Header("Scene References")]
    public GameObject ovrCameraRig;
    public GameObject playerBird;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.5f;

    [Header("Background Audio")]
    [SerializeField] private AudioClip scene1BgmClip;
    [SerializeField] private AudioClip scene23BgmClip;
    [SerializeField] private AudioClip forestAmbientClip;
    [SerializeField, Range(0f, 1f)] private float mainBgmVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float forestAmbientVolume = 0.12f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip footstepClip;

    [System.Serializable]
    public struct SceneConfig
    {
        public string sceneName;
        public bool isBirdActive;
        public bool useFloorLevel;
        public Vector3 cameraPos;
        public Vector3 cameraRot;
    }

    [Header("Scene Sequence")]
    public List<SceneConfig> sceneSequence = new List<SceneConfig>();

    private int currentSequenceIndex = -1;
    private bool isSwitching = false;
    private bool isFirst = true;
    private AudioSource mainBgmSource;
    private AudioSource ambientBgmSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (ovrCameraRig != null)
            {
                ovrCameraRig.transform.SetParent(null);
                DontDestroyOnLoad(ovrCameraRig);
                Debug.Log("OVR camera rig marked as DontDestroyOnLoad.");
            }

            EnsureBackgroundAudioSources();
            ApplyBackgroundAudioSettings();
            EnsureFootstepAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        ApplyBackgroundAudioSettings();
    }

    public void StartGameSequence()
    {
        Debug.Log("Starting scene sequence.");
        SwitchToNextScene();
    }

    public void SetJumpGameMode(bool active)
    {
        if (ovrCameraRig == null)
        {
            return;
        }

        var bird = ovrCameraRig.GetComponentInChildren<birdController>();
        if (bird != null)
        {
            bird.isJumpGame = active;
        }
    }

    public void SwitchToNextScene()
    {
        if (isSwitching)
        {
            Debug.LogWarning("Scene switch already in progress.");
            return;
        }

        currentSequenceIndex++;

        if (currentSequenceIndex < sceneSequence.Count)
        {
            SceneConfig nextConfig = sceneSequence[currentSequenceIndex];
            Debug.Log($"Loading next scene in sequence: {nextConfig.sceneName}");
            StartCoroutine(LoadSceneRoutine(nextConfig));

            if (nextConfig.sceneName == "Forest1")
            {
                SetJumpGameMode(true);
            }
            else
            {
                SetJumpGameMode(false);
            }
        }
        else
        {
            Debug.Log("Scene sequence completed.");
        }
    }

    public void SwitchToSceneByName(string name)
    {
        SceneConfig config = sceneSequence.Find(s => s.sceneName == name);
        if (config.sceneName != null)
        {
            StartCoroutine(LoadSceneRoutine(config));
        }
    }

    private IEnumerator LoadSceneRoutine(SceneConfig config)
    {
        isSwitching = true;

        if (isFirst)
        {
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
            }

            isFirst = false;
        }
        else
        {
            yield return StartCoroutine(Fade(1f));
        }

        Debug.Log($"[Scene] Loading: {config.sceneName}");

        AsyncOperation op = SceneManager.LoadSceneAsync(config.sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        ApplyConfig(config);
        UpdateBackgroundAudio(config.sceneName);

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(Fade(0f));

        isSwitching = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void SetTrackingOrigin(bool useFloorLevel)
    {
        if (OVRManager.instance == null)
        {
            return;
        }

        OVRManager.instance.trackingOriginType = useFloorLevel
            ? OVRManager.TrackingOrigin.FloorLevel
            : OVRManager.TrackingOrigin.EyeLevel;

        OVRManager.display.RecenterPose();
        Debug.Log($"[VR] Tracking origin set to {(useFloorLevel ? "Floor Level" : "Eye Level")}");
    }

    private void ApplyConfig(SceneConfig config)
    {
        if (ovrCameraRig == null)
        {
            return;
        }

        ovrCameraRig.SetActive(true);
        SetTrackingOrigin(config.useFloorLevel);

        Rigidbody rb = ovrCameraRig.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        ovrCameraRig.transform.position = config.cameraPos;
        ovrCameraRig.transform.eulerAngles = config.cameraRot;

        Physics.SyncTransforms();

        if (rb != null)
        {
            rb.useGravity = config.isBirdActive;
            rb.isKinematic = !config.isBirdActive;

            if (config.isBirdActive)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        var birdCtrl = ovrCameraRig.GetComponentInChildren<birdController>();
        if (birdCtrl != null)
        {
            birdCtrl.enabled = config.isBirdActive;
        }

        if (playerBird != null)
        {
            playerBird.SetActive(config.isBirdActive);
            if (config.sceneName == "Forest1")
            {
                playerBird.SetActive(false);
            }
        }

        Debug.Log($"[Scene] {config.sceneName} configured for {(config.useFloorLevel ? "Floor" : "Eye")} mode");
    }

    private void EnsureBackgroundAudioSources()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();

        if (audioSources.Length > 0)
        {
            mainBgmSource = audioSources[0];
        }
        else
        {
            mainBgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSources.Length > 1)
        {
            ambientBgmSource = audioSources[1];
        }
        else
        {
            ambientBgmSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ApplyBackgroundAudioSettings()
    {
        if (mainBgmSource != null)
        {
            ConfigureLoopingAudioSource(mainBgmSource, mainBgmVolume);
        }

        if (ambientBgmSource != null)
        {
            ConfigureLoopingAudioSource(ambientBgmSource, forestAmbientVolume);
        }
    }

    private void EnsureFootstepAudio()
    {
        if (footstepClip == null || ovrCameraRig == null)
        {
            return;
        }

        Camera centerEyeCamera = ovrCameraRig.GetComponentInChildren<Camera>(true);
        if (centerEyeCamera == null)
        {
            return;
        }

        GameObject footstepObject = centerEyeCamera.gameObject;
        AudioSource footstepSource = footstepObject.GetComponent<AudioSource>();
        if (footstepSource == null)
        {
            footstepSource = footstepObject.AddComponent<AudioSource>();
        }

        CenterEyeDistanceFootsteps footsteps = footstepObject.GetComponent<CenterEyeDistanceFootsteps>();
        if (footsteps == null)
        {
            footsteps = footstepObject.AddComponent<CenterEyeDistanceFootsteps>();
        }

        footsteps.Initialize(centerEyeCamera.transform, footstepSource, footstepClip);
    }

    private void ConfigureLoopingAudioSource(AudioSource source, float volume)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = volume;
    }

    private void UpdateBackgroundAudio(string sceneName)
    {
        int sceneIndex = GetSceneSequenceIndex(sceneName);
        AudioClip mainClip = null;

        if (sceneIndex == 0)
        {
            mainClip = scene1BgmClip;
        }
        else if (sceneIndex == 1 || sceneIndex == 2)
        {
            mainClip = scene23BgmClip;
        }

        bool shouldPlayForestAmbient = sceneIndex >= 0 && sceneIndex <= 2;

        PlayLoopingClip(mainBgmSource, mainClip);
        PlayLoopingClip(ambientBgmSource, shouldPlayForestAmbient ? forestAmbientClip : null);
    }

    private int GetSceneSequenceIndex(string sceneName)
    {
        for (int i = 0; i < sceneSequence.Count; i++)
        {
            if (sceneSequence[i].sceneName == sceneName)
            {
                return i;
            }
        }

        return -1;
    }

    private void PlayLoopingClip(AudioSource source, AudioClip clip)
    {
        if (source == null)
        {
            return;
        }

        if (clip == null)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }

            source.clip = null;
            return;
        }

        bool clipChanged = source.clip != clip;
        if (clipChanged)
        {
            source.clip = clip;
        }

        if (clipChanged || !source.isPlaying)
        {
            source.Play();
        }
    }
}
