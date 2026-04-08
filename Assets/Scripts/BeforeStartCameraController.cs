using UnityEngine;

public class BeforeStartCameraController : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;

    void Update()
    {
        // Positive values rotate one way, negative values rotate the opposite way.
        float angleDelta = rotationSpeed * Time.deltaTime;
        transform.Rotate(0f, angleDelta, 0f, Space.Self);
    }
}
