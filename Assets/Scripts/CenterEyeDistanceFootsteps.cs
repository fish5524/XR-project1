using UnityEngine;

[DisallowMultipleComponent]
public class CenterEyeDistanceFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform trackedTransform;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip footstepClip;

    [Header("Distance")]
    [SerializeField] private float stepDistance = 0.55f;
    [SerializeField] private float minFrameDistance = 0.002f;
    [SerializeField] private float ignoreDistanceAbove = 1.5f;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volume = 1.0f;
    [SerializeField] private float minPitch = 0.97f;
    [SerializeField] private float maxPitch = 1.03f;

    private Vector3 lastWorldPosition;
    private float accumulatedDistance;

    private void Awake()
    {
        if (trackedTransform == null && Camera.main != null)
        {
            trackedTransform = Camera.main.transform;
        }

        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
        ResetTracking();
    }

    private void OnEnable()
    {
        ResetTracking();
    }

    private void OnValidate()
    {
        stepDistance = Mathf.Max(0.01f, stepDistance);
        minFrameDistance = Mathf.Max(0f, minFrameDistance);
        ignoreDistanceAbove = Mathf.Max(stepDistance, ignoreDistanceAbove);

        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
        }

        if (footstepSource != null)
        {
            ConfigureAudioSource();
        }
    }

    private void Update()
    {
        if (trackedTransform == null || footstepSource == null || footstepClip == null)
        {
            return;
        }

        Vector3 currentWorldPosition = trackedTransform.position;
        Vector3 frameDelta = currentWorldPosition - lastWorldPosition;
        lastWorldPosition = currentWorldPosition;

        frameDelta.y = 0f;
        float horizontalDistance = frameDelta.magnitude;

        if (horizontalDistance < minFrameDistance)
        {
            return;
        }

        if (horizontalDistance > ignoreDistanceAbove)
        {
            accumulatedDistance = 0f;
            return;
        }

        accumulatedDistance += horizontalDistance;

        if (accumulatedDistance < stepDistance)
        {
            return;
        }

        accumulatedDistance -= stepDistance;
        footstepSource.pitch = Random.Range(minPitch, maxPitch);
        footstepSource.PlayOneShot(footstepClip, volume);
    }

    private void ResetTracking()
    {
        accumulatedDistance = 0f;

        if (trackedTransform != null)
        {
            lastWorldPosition = trackedTransform.position;
        }
    }

    private void ConfigureAudioSource()
    {
        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.dopplerLevel = 0f;
        footstepSource.priority = 32;
        footstepSource.clip = footstepClip;
    }
}
