using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PipeManager : MonoBehaviour
{
    [Header("Pipe Prefabs")]
    public PipeSegment[] pipePrefabs;

    [Header("Spawning")]
    [Min(1)]
    public int startingSegments = 3;

    [Tooltip("Spawn a new segment when the newest connector reaches this world Z position.")]
    public float spawnAheadZ = 600f;

    [Header("Speed Progression")]
    [FormerlySerializedAs("moveSpeed")]
    [Min(0f)]
    public float startingMoveSpeed = 30f;

    [Min(0f)]
    public float accelerationPerSecond = 0.75f;

    [Min(0f)]
    public float maximumMoveSpeed = 70f;

    [Header("Obstacle Density")]
    [Tooltip("Fraction of authored obstacles shown at the beginning.")]
    [Range(0f, 1f)]
    public float startingObstacleVisibility = 0.2f;

    [Tooltip("Fraction of authored obstacles shown at maximum speed.")]
    [Range(0f, 1f)]
    public float maximumObstacleVisibility = 0.9f;

    [Tooltip("Randomly varies the visible obstacle count by this amount.")]
    [Min(0)]
    public int obstacleCountVariance = 2;

    [Min(0)]
    public int minimumVisibleObstacles = 1;

    public float CurrentMoveSpeed { get; private set; }
    public float CurrentObstacleVisibility { get; private set; }

    private readonly List<PipeSegment> activeSegments = new();
    private PipeSegment lastSegment;
    private bool gameRunning = true;

    private void Start()
    {
        CurrentMoveSpeed = startingMoveSpeed;
        CurrentObstacleVisibility = startingObstacleVisibility;

        Vector3 connectorPosition = transform.position;
        Quaternion connectorRotation = transform.rotation;

        for (int i = 0; i < startingSegments; i++)
        {
            PipeSegment segment = SpawnSegment(connectorPosition, connectorRotation);
            if (segment == null)
                break;

            connectorPosition = segment.spawnPoint.position;
            connectorRotation = segment.spawnPoint.rotation;
        }
    }

    private void Update()
    {
        if (!gameRunning)
            return;

        CurrentMoveSpeed = Mathf.Min(
            maximumMoveSpeed,
            CurrentMoveSpeed + accelerationPerSecond * Time.deltaTime
        );

        float speedProgress = Mathf.InverseLerp(
            startingMoveSpeed,
            maximumMoveSpeed,
            CurrentMoveSpeed
        );
        CurrentObstacleVisibility = Mathf.Lerp(
            startingObstacleVisibility,
            maximumObstacleVisibility,
            speedProgress
        );

        float moveDistance = CurrentMoveSpeed * Time.deltaTime;

        // Move every segment by exactly the same distance in the same frame.
        // This prevents seams from appearing as the game accelerates.
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            PipeSegment segment = activeSegments[i];

            if (segment == null)
            {
                activeSegments.RemoveAt(i);
                continue;
            }

            segment.Move(moveDistance);

            if (segment.IsPastDestroyPoint())
            {
                activeSegments.RemoveAt(i);
                Destroy(segment.gameObject);
            }
        }

        // Distance-based spawning remains consistent at every speed. The new
        // prefab is placed directly on the previous prefab's connector.
        while (lastSegment != null &&
               lastSegment.spawnPoint.position.z <= spawnAheadZ)
        {
            PipeSegment segment = SpawnSegment(
                lastSegment.spawnPoint.position,
                lastSegment.spawnPoint.rotation
            );

            if (segment == null)
                break;
        }
    }

    private PipeSegment SpawnSegment(Vector3 position, Quaternion rotation)
    {
        if (pipePrefabs == null || pipePrefabs.Length == 0)
        {
            Debug.LogError("PipeManager needs at least one pipe prefab.", this);
            return null;
        }

        int randomIndex = Random.Range(0, pipePrefabs.Length);
        PipeSegment prefab = pipePrefabs[randomIndex];

        if (prefab == null)
        {
            Debug.LogError($"Pipe prefab slot {randomIndex} is empty.", this);
            return null;
        }

        PipeSegment segment = Instantiate(prefab, position, rotation);

        if (segment.spawnPoint == null)
        {
            Debug.LogError($"{segment.name} has no SpawnPoint assigned.", segment);
            Destroy(segment.gameObject);
            return null;
        }

        segment.RandomizeObstacles(
            CurrentObstacleVisibility,
            obstacleCountVariance,
            minimumVisibleObstacles
        );

        activeSegments.Add(segment);
        lastSegment = segment;
        return segment;
    }

    public void StopAllPipes()
    {
        gameRunning = false;
    }
}
