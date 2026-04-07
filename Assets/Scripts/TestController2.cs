using UnityEngine;
using System.Collections;

public class TestController2 : MonoBehaviour
{
    [SerializeField] private FlyingSceneManager targetSceneManager;
    [SerializeField] private float countdownSeconds = 10f;

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (targetSceneManager == null)
        {
            Debug.LogWarning("TestController2: targetSceneManager is not assigned.");
            yield break;
        }

        yield return Countdown("PlayHoveringAnimation", countdownSeconds);
        targetSceneManager.PlayHoveringAnimation();

        yield return Countdown("PlayLastAnimation", countdownSeconds);
        targetSceneManager.PlayLastAnimation();
    }

    private IEnumerator Countdown(string label, float seconds)
    {
        int remaining = Mathf.CeilToInt(Mathf.Max(0f, seconds));

        while (remaining > 0)
        {
            Debug.Log($"{label} countdown: {remaining}s remaining");
            yield return new WaitForSeconds(1f);
            remaining--;
        }
    }
}
