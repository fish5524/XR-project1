using UnityEngine;

public class BirdAgent : MonoBehaviour
{
    public void Move(Vector3 desiredDirection, float moveSpeed, float turnSpeed)
    {
        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}
