using UnityEngine;
using System;

public class GameCore : MonoBehaviour
{
    public enum CoreState
    {
        Idle,
        PlayingStory,
        WaitingForInteraction,
        Completed
    }

    [Header("Flow Config")]
    [SerializeField] private bool autoStartOnPlay = true;

    private const int StoryCount = 3;
    private const int InteractionCount = 2;

    private int currentStoryIndex;
    private int currentInteractionIndex;

    public CoreState CurrentState { get; private set; } = CoreState.Idle;

    // Core callbacks
    public event Action<CoreState> OnStateChanged;
    public event Action<int> OnStoryStarted;
    public event Action<int> OnStoryCompleted;
    public event Action<int> OnInteractionRequested;
    public event Action<int> OnInteractionCompleted;
    public event Action OnFlowCompleted;

    private void Start()
    {
        OnStoryStarted += (index) => GlobalSceneController.instance.SwitchToNextScene();
        OnInteractionRequested += (index) => GlobalSceneController.instance.SwitchToNextScene();
        if (autoStartOnPlay)
        {
            StartFlow();
        }
    }

    public void StartFlow()
    {
        currentStoryIndex = 0;
        currentInteractionIndex = 0;
        PlayCurrentStory();
    }

    // Called by your story script when one story segment is finished.
    public void NotifyStoryFinished()
    {
        if (CurrentState != CoreState.PlayingStory)
        {
            Debug.LogWarning("NotifyStoryFinished called while not in PlayingStory state.");
            return;
        }

        int storyNumber = currentStoryIndex + 1;
        OnStoryCompleted?.Invoke(storyNumber);
        Debug.Log($"Story {storyNumber} completed.");

        bool hasMoreInteraction = currentInteractionIndex < InteractionCount;
        if (hasMoreInteraction)
        {
            RequestCurrentInteraction();
            return;
        }

        SetState(CoreState.Completed);
        OnFlowCompleted?.Invoke();
        Debug.Log("All story segments completed.");
    }

    // Called by your interaction script when the required interaction is done.
    public void NotifyInteractionFinished()
    {
        if (CurrentState != CoreState.WaitingForInteraction)
        {
            Debug.LogWarning("NotifyInteractionFinished called while not in WaitingForInteraction state.");
            return;
        }

        int interactionNumber = currentInteractionIndex + 1;
        OnInteractionCompleted?.Invoke(interactionNumber);
        Debug.Log($"Interaction {interactionNumber} completed.");

        currentInteractionIndex++;

        if (currentStoryIndex >= StoryCount)
        {
            SetState(CoreState.Completed);
            OnFlowCompleted?.Invoke();
            return;
        }

        PlayCurrentStory();
    }

    private void PlayCurrentStory()
    {
        if (currentStoryIndex >= StoryCount)
        {
            SetState(CoreState.Completed);
            OnFlowCompleted?.Invoke();
            return;
        }

        SetState(CoreState.PlayingStory);

        int storyNumber = currentStoryIndex + 1;
        OnStoryStarted?.Invoke(storyNumber);
        Debug.Log($"Start story {storyNumber}.");

        // TODO: Replace with your own story script calls.
        // if (storyNumber == 1) storyPhase1.Play();
        // if (storyNumber == 2) storyPhase2.Play();
        // if (storyNumber == 3) storyPhase3.Play();

        currentStoryIndex++;
    }

    private void RequestCurrentInteraction()
    {
        if (currentInteractionIndex >= InteractionCount)
        {
            SetState(CoreState.Completed);
            OnFlowCompleted?.Invoke();
            return;
        }

        SetState(CoreState.WaitingForInteraction);

        int interactionNumber = currentInteractionIndex + 1;
        OnInteractionRequested?.Invoke(interactionNumber);
        Debug.Log($"Waiting for interaction {interactionNumber}.");

        // TODO: Replace with your own interaction script calls.
        // if (interactionNumber == 1) interactionPhase1.BeginListen();
        // if (interactionNumber == 2) interactionPhase2.BeginListen();
    }

    private void SetState(CoreState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        OnStateChanged?.Invoke(CurrentState);
    }
}
