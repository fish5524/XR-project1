using UnityEngine;

public class FlyingSceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BirdManager birdManager;
    [SerializeField] private Transform lastAnimationTrajectoryRoot;

    public void PlayHoveringAnimation()
    {
        if (birdManager == null)
        {
            Debug.LogWarning("FlyingSceneManager: BirdManager is not assigned.");
            return;
        }

        birdManager.StartFlock();
    }

    public void PlayLastAnimation()
    {
        if (birdManager == null)
        {
            Debug.LogWarning("FlyingSceneManager: BirdManager is not assigned.");
            return;
        }

        if (lastAnimationTrajectoryRoot == null)
        {
            Debug.LogWarning("FlyingSceneManager: lastAnimationTrajectoryRoot is not assigned.");
            return;
        }

        birdManager.StopFlock();
        birdManager.SetTrajectoryRoot(lastAnimationTrajectoryRoot);
        birdManager.StartFlock();
    }
}
