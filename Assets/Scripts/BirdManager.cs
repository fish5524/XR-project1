using UnityEngine;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class BirdManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform birdsRoot;
    [SerializeField] private Transform trajectoryRoot;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float waypointReachDistance = 2.5f;
    [SerializeField] private bool loopPath = true;

    [Header("Noise")]
    [SerializeField] private float randomDirectionWeight = 0.4f;
    [SerializeField] private float randomDirectionFrequency = 0.8f;

    [Header("Collision Avoidance")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationWeight = 1.2f;

    private readonly List<BirdAgent> birds = new List<BirdAgent>();
    private readonly List<Transform> waypoints = new List<Transform>();
    private readonly List<Vector3> noiseSeeds = new List<Vector3>();

    private int currentWaypointIndex;
    private bool destinationReachedThisRun;

    public bool IsRunning { get; private set; }
    public event Action OnDestinationReached;

    private void Start()
    {
        RebuildBirdList();
        RebuildWaypointList();
        IsRunning = playOnStart;
        destinationReachedThisRun = false;

        if (birds.Count == 0)
        {
            UnityEngine.Debug.LogWarning("BirdManager: No birds found under birdsRoot.");
        }

        if (waypoints.Count == 0)
        {
            UnityEngine.Debug.LogWarning("BirdManager: No waypoint found under trajectoryRoot.");
        }
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (birds.Count == 0 || waypoints.Count == 0)
        {
            return;
        }

        TryAdvanceWaypoint();

        Vector3 waypointPosition = waypoints[currentWaypointIndex].position;

        for (int i = 0; i < birds.Count; i++)
        {
            BirdAgent bird = birds[i];
            if (bird == null)
            {
                continue;
            }

            Vector3 toWaypoint = (waypointPosition - bird.transform.position).normalized;
            Vector3 randomDirection = GetRandomDirection(i) * randomDirectionWeight;
            Vector3 separationDirection = GetSeparationDirection(i) * separationWeight;

            Vector3 desiredDirection = toWaypoint + randomDirection + separationDirection;
            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                desiredDirection = bird.transform.forward;
            }

            bird.Move(desiredDirection.normalized, moveSpeed, turnSpeed);
        }
    }

    [ContextMenu("Rebuild Bird List")]
    public void RebuildBirdList()
    {
        birds.Clear();
        noiseSeeds.Clear();

        if (birdsRoot == null)
        {
            return;
        }

        Transform[] allChildren = birdsRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform t = allChildren[i];
            if (t == birdsRoot)
            {
                continue;
            }

            BirdAgent agent = t.GetComponent<BirdAgent>();
            if (agent == null)
            {
                continue;
            }

            birds.Add(agent);
            noiseSeeds.Add(new Vector3(UnityEngine.Random.value * 10f, UnityEngine.Random.value * 10f, UnityEngine.Random.value * 10f));
        }
    }

    [ContextMenu("Rebuild Waypoint List")]
    public void RebuildWaypointList()
    {
        waypoints.Clear();
        currentWaypointIndex = 0;

        if (trajectoryRoot == null)
        {
            return;
        }

        int childCount = trajectoryRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            waypoints.Add(trajectoryRoot.GetChild(i));
        }
    }

    public void SetTrajectoryRoot(Transform newTrajectoryRoot)
    {
        trajectoryRoot = newTrajectoryRoot;
        RebuildWaypointList();
    }

    [ContextMenu("Start Flock")]
    public void StartFlock()
    {
        if (waypoints.Count == 0)
        {
            RebuildWaypointList();
        }

        if (birds.Count == 0)
        {
            RebuildBirdList();
        }

        destinationReachedThisRun = false;
        IsRunning = birds.Count > 0 && waypoints.Count > 0;
    }

    [ContextMenu("Pause Flock")]
    public void PauseFlock()
    {
        IsRunning = false;
    }

    [ContextMenu("Resume Flock")]
    public void ResumeFlock()
    {
        StartFlock();
    }

    [ContextMenu("Stop Flock")]
    public void StopFlock()
    {
        IsRunning = false;
        currentWaypointIndex = 0;
        destinationReachedThisRun = false;
    }

    private void TryAdvanceWaypoint()
    {
        Vector3 center = GetFlockCenter();
        float distance = Vector3.Distance(center, waypoints[currentWaypointIndex].position);
        if (distance > waypointReachDistance)
        {
            return;
        }

        if (currentWaypointIndex < waypoints.Count - 1)
        {
            currentWaypointIndex++;
            return;
        }

        if (loopPath)
        {
            currentWaypointIndex = 0;
            return;
        }

        if (!destinationReachedThisRun)
        {
            destinationReachedThisRun = true;
            IsRunning = false;
            OnDestinationReached?.Invoke();
        }
    }

    private Vector3 GetFlockCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < birds.Count; i++)
        {
            BirdAgent bird = birds[i];
            if (bird == null)
            {
                continue;
            }

            sum += bird.transform.position;
            count++;
        }

        if (count == 0)
        {
            return transform.position;
        }

        return sum / count;
    }

    private Vector3 GetRandomDirection(int index)
    {
        Vector3 seed = noiseSeeds[index];
        float t = Time.time * randomDirectionFrequency;

        Vector3 dir = new Vector3(
            Mathf.Sin(t + seed.x * 1.91f),
            Mathf.Sin(t * 1.37f + seed.y * 2.17f),
            Mathf.Sin(t * 0.73f + seed.z * 2.63f)
        );

        return dir.normalized;
    }

    private Vector3 GetSeparationDirection(int index)
    {
        BirdAgent me = birds[index];
        if (me == null)
        {
            return Vector3.zero;
        }

        Vector3 separation = Vector3.zero;
        Vector3 myPosition = me.transform.position;
        float separationRadiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < birds.Count; i++)
        {
            if (i == index)
            {
                continue;
            }

            BirdAgent other = birds[i];
            if (other == null)
            {
                continue;
            }

            Vector3 offset = myPosition - other.transform.position;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > 0.0001f && sqrDistance < separationRadiusSqr)
            {
                separation += offset.normalized / sqrDistance;
            }
        }

        if (separation.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return separation.normalized;
    }
}
