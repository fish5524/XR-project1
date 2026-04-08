using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BirdAgent : MonoBehaviour
{
    private const string IdleClip = "AmazonMacaw_Rig:ParrotAnimated|Parrot_Idle";
    private const string HopClip = "AmazonMacaw_Rig:ParrotAnimated|Parrot_Hop";
    private const string EatClip = "AmazonMacaw_Rig:ParrotAnimated|Parrot_Eat";
    private const string TakeOffClip = "AmazonMacaw_Rig:ParrotAnimated|Parrot_TakeOff";
    private const string FlapClip = "AmazonMacaw_Rig:ParrotAnimated|Parrot_Flap";

    public enum MainState
    {
        Idle,
        Flying
    }

    public enum IdleAction
    {
        None,
        Idle,
        HorizontalMove,
        Eat
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource ambientCallAudioSource;

    [Header("Idle Behavior")]
    [SerializeField] private List<BirdAgent> sameTypeAgents = new List<BirdAgent>();
    [SerializeField] private float horizontalMoveRange = 15f;
    [SerializeField] private float horizontalHopDistance = 0.35f;
    [SerializeField] private float headingJitterDegrees = 12f;
    [SerializeField] private float finalHopSpeedMultiplier = 0.8f;
    [SerializeField] private float horizontalMoveArriveDistance = 0.05f;

    [Header("Flying")]
    [SerializeField] private float takeOffDuration = 0.8f;

    [Header("Runtime State")]
    [SerializeField] private MainState currentMainState = MainState.Idle;
    [SerializeField] private IdleAction currentIdleAction = IdleAction.None;

    public MainState CurrentMainState => currentMainState;
    public IdleAction CurrentIdleAction => currentIdleAction;

    private Coroutine idleRoutine;
    private Coroutine ambientCallRoutine;
    private float takeOffEndTime;
    private bool isFlapLoopPlaying;

    private float idleClipLength = 1f;
    private float eatClipLength = 1f;
    private float hopClipLength = 0.5f;

    private AudioClip ambientCallClip;
    private bool ambientCallsEnabled;
    private float ambientCallMinInterval = 4f;
    private float ambientCallMaxInterval = 9f;
    private float ambientCallVolume = 0.35f;
    private float ambientCallMinPitch = 0.95f;
    private float ambientCallMaxPitch = 1.05f;
    private float ambientCallMinDistance = 2f;
    private float ambientCallMaxDistance = 18f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        CacheAnimationLengths();
        EnterIdleState();
    }

    private void OnEnable()
    {
        if (ambientCallsEnabled && ambientCallClip != null)
        {
            RestartAmbientCallLoop();
        }
    }

    private void OnDisable()
    {
        StopAmbientCallLoop();

        if (ambientCallAudioSource != null)
        {
            ambientCallAudioSource.Stop();
        }
    }

    private void Update()
    {
        if (currentMainState == MainState.Flying && !isFlapLoopPlaying && Time.time >= takeOffEndTime)
        {
            PlayAnimation(FlapClip);
            isFlapLoopPlaying = true;
        }
    }

    public void Move(Vector3 desiredDirection, float moveSpeed, float turnSpeed)
    {
        if (currentMainState != MainState.Flying)
        {
            EnterFlyingState();
        }

        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    public void Land()
    {
        EnterIdleState();
    }

    public void ConfigureAmbientCall(
        AudioClip clip,
        bool enabled,
        float minInterval,
        float maxInterval,
        float volume,
        float minPitch,
        float maxPitch,
        float minDistance,
        float maxDistance)
    {
        ambientCallClip = clip;
        ambientCallsEnabled = enabled;
        ambientCallMinInterval = Mathf.Max(0.1f, minInterval);
        ambientCallMaxInterval = Mathf.Max(ambientCallMinInterval, maxInterval);
        ambientCallVolume = Mathf.Clamp01(volume);
        ambientCallMinPitch = Mathf.Max(0.1f, minPitch);
        ambientCallMaxPitch = Mathf.Max(ambientCallMinPitch, maxPitch);
        ambientCallMinDistance = Mathf.Max(0.1f, minDistance);
        ambientCallMaxDistance = Mathf.Max(ambientCallMinDistance, maxDistance);

        if (!ambientCallsEnabled || ambientCallClip == null)
        {
            StopAmbientCallLoop();

            if (ambientCallAudioSource != null)
            {
                ambientCallAudioSource.Stop();
                ambientCallAudioSource.enabled = false;
            }

            return;
        }

        EnsureAmbientCallAudioSource();
        ConfigureAmbientCallAudioSource();
        ambientCallAudioSource.enabled = true;
        RestartAmbientCallLoop();
    }

    private void EnterIdleState()
    {
        currentMainState = MainState.Idle;
        isFlapLoopPlaying = false;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
        }

        idleRoutine = StartCoroutine(IdleLoop());
    }

    private void EnterFlyingState()
    {
        currentMainState = MainState.Flying;
        currentIdleAction = IdleAction.None;
        isFlapLoopPlaying = false;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        PlayAnimation(TakeOffClip);
        takeOffEndTime = Time.time + Mathf.Max(0.01f, takeOffDuration);
    }

    private IEnumerator IdleLoop()
    {
        while (currentMainState == MainState.Idle)
        {
            float actionRoll = Random.value;

            if (actionRoll < 0.6f)
            {
                currentIdleAction = IdleAction.Idle;
                PlayAnimation(IdleClip);
                yield return new WaitForSeconds(idleClipLength);
                continue;
            }

            if (actionRoll < 0.9f)
            {
                currentIdleAction = IdleAction.Eat;
                PlayAnimation(EatClip);
                yield return new WaitForSeconds(eatClipLength);
                continue;
            }

            if (actionRoll <= 1f)
            {
                currentIdleAction = IdleAction.HorizontalMove;
                yield return HorizontalMoveRoutine();
                continue;
            }
        }
    }

    private IEnumerator HorizontalMoveRoutine()
    {
        Vector3 groupCenter = GetGroupCenterPosition();
        Vector2 randomCircle = Random.insideUnitCircle * Mathf.Max(0f, horizontalMoveRange);
        Vector3 targetPosition = new Vector3(
            groupCenter.x + randomCircle.x,
            transform.position.y,
            groupCenter.z + randomCircle.y
        );

        float hopDuration = Mathf.Max(0.05f, hopClipLength);

        while (currentMainState == MainState.Idle)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= horizontalMoveArriveDistance * horizontalMoveArriveDistance)
            {
                transform.position = targetPosition;
                yield break;
            }

            Vector3 moveDir = toTarget.normalized;

            float jitterYaw = Random.Range(-headingJitterDegrees, headingJitterDegrees);
            Vector3 jitteredDir = Quaternion.AngleAxis(jitterYaw, Vector3.up) * moveDir;
            if (jitteredDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(jitteredDir.normalized, Vector3.up);
            }

            PlayAnimation(HopClip);

            float remainingDistance = toTarget.magnitude;
            bool isLastHop = remainingDistance <= horizontalHopDistance;
            float stepDistance = isLastHop ? remainingDistance : horizontalHopDistance;
            Vector3 hopStart = transform.position;
            Vector3 hopEnd = hopStart + moveDir * stepDistance;

            float safeMultiplier = Mathf.Max(0.1f, finalHopSpeedMultiplier);
            float hopMoveSpeed = stepDistance / hopDuration;
            if (isLastHop)
            {
                hopMoveSpeed *= safeMultiplier;
            }

            float elapsed = 0f;
            while (elapsed < hopDuration && currentMainState == MainState.Idle)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, hopEnd, hopMoveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = hopEnd;
        }
    }

    private Vector3 GetGroupCenterPosition()
    {
        Vector3 sum = transform.position;
        int count = 1;

        for (int i = 0; i < sameTypeAgents.Count; i++)
        {
            BirdAgent agent = sameTypeAgents[i];
            if (agent == null || agent == this)
            {
                continue;
            }

            sum += agent.transform.position;
            count++;
        }

        return sum / count;
    }

    private void CacheAnimationLengths()
    {
        idleClipLength = GetClipLength(IdleClip, 1f);
        eatClipLength = GetClipLength(EatClip, 1f);
        hopClipLength = GetClipLength(HopClip, 0.5f);
    }

    private float GetClipLength(string clipName, float fallback)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return fallback;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == clipName)
            {
                return Mathf.Max(0.01f, clips[i].length);
            }
        }

        int splitIndex = clipName.LastIndexOf('|');
        string shortName = splitIndex >= 0 ? clipName.Substring(splitIndex + 1) : clipName;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name.EndsWith(shortName))
            {
                return Mathf.Max(0.01f, clips[i].length);
            }
        }

        return fallback;
    }

    private void PlayAnimation(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateName, 0.1f);
        animator.speed = 1f;
    }

    private void EnsureAmbientCallAudioSource()
    {
        if (ambientCallAudioSource == null)
        {
            ambientCallAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureAmbientCallAudioSource()
    {
        ambientCallAudioSource.playOnAwake = false;
        ambientCallAudioSource.loop = false;
        ambientCallAudioSource.clip = ambientCallClip;
        ambientCallAudioSource.spatialBlend = 1f;
        ambientCallAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        ambientCallAudioSource.minDistance = ambientCallMinDistance;
        ambientCallAudioSource.maxDistance = ambientCallMaxDistance;
        ambientCallAudioSource.dopplerLevel = 0f;
        ambientCallAudioSource.priority = 64;
        ambientCallAudioSource.spread = 0f;
        ambientCallAudioSource.volume = 1f;
    }

    private void RestartAmbientCallLoop()
    {
        StopAmbientCallLoop();

        if (isActiveAndEnabled)
        {
            ambientCallRoutine = StartCoroutine(AmbientCallLoop());
        }
    }

    private void StopAmbientCallLoop()
    {
        if (ambientCallRoutine == null)
        {
            return;
        }

        StopCoroutine(ambientCallRoutine);
        ambientCallRoutine = null;
    }

    private IEnumerator AmbientCallLoop()
    {
        float initialDelay = Random.Range(0.05f, Mathf.Max(0.05f, ambientCallMaxInterval));
        yield return new WaitForSeconds(initialDelay);

        while (ambientCallsEnabled && ambientCallClip != null)
        {
            if (ambientCallAudioSource != null && !ambientCallAudioSource.isPlaying)
            {
                ambientCallAudioSource.pitch = Random.Range(ambientCallMinPitch, ambientCallMaxPitch);
                ambientCallAudioSource.PlayOneShot(ambientCallClip, ambientCallVolume);
            }

            yield return new WaitForSeconds(Random.Range(ambientCallMinInterval, ambientCallMaxInterval));
        }

        ambientCallRoutine = null;
    }
}
