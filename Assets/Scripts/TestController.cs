using UnityEngine;
using System.Collections;

public class TestController : MonoBehaviour
{
    [SerializeField] private EggNestAnimationController eggNestAnimationController;

    void Start()
    {
        // 如果在 Inspector 沒有分配，自動從同物件取得
        if (eggNestAnimationController == null)
        {
            eggNestAnimationController = GetComponent<EggNestAnimationController>();
        }

        // 開始倒數計時，然後呼叫動畫函數
        if (eggNestAnimationController != null)
        {
            StartCoroutine(CountdownThenStartAnimation());
        }
        else
        {
            Debug.LogError("EggNestAnimationController not found!");
        }
    }

    private IEnumerator CountdownThenStartAnimation()
    {
        // 倒數計時 10 秒
        for (int i = 1; i > 0; i--)
        {
            Debug.Log($"倒數計時: {i} 秒");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("開始動畫!");
        eggNestAnimationController.StartAnimation(() =>
        {
            Debug.Log("EggNestAnimationController callback called.");
        });
    }

    void Update()
    {
        
    }
}
