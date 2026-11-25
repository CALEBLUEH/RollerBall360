using System.Collections.Generic;
using UnityEngine;

public class PipeSegment : MonoBehaviour
{
    [Header("References")]
    public Transform spawnPoint;

    [Header("Settings")]
    public float destroyZ = -300f;

    [Tooltip("Authoring value used by the prefab generator for consistent Z-axis obstacle spacing.")]
    [Range(2f, 10f)]
    public float obstacleSpacing = 5f;

    public void RandomizeObstacles(float visibleFraction, int countVariance, int minimumVisible)
    {
        List<GameObject> obstacleGroups = new();

        // Each manually-authored root obstacle group is treated as one unit.
        // Removing groups can only open the guaranteed route; it never moves
        // an obstacle into it.
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("ObstacleGroup") ||
                child.name.StartsWith("ObstacleArrangement"))
            {
                obstacleGroups.Add(child.gameObject);
            }
        }

        if (obstacleGroups.Count == 0)
            return;

        for (int i = obstacleGroups.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (obstacleGroups[i], obstacleGroups[randomIndex]) =
                (obstacleGroups[randomIndex], obstacleGroups[i]);
        }

        int randomAdjustment = Random.Range(-countVariance, countVariance + 1);
        int visibleCount = Mathf.RoundToInt(obstacleGroups.Count * visibleFraction) +
                           randomAdjustment;
        visibleCount = Mathf.Clamp(
            visibleCount,
            Mathf.Min(minimumVisible, obstacleGroups.Count),
            obstacleGroups.Count
        );

        for (int i = 0; i < obstacleGroups.Count; i++)
            obstacleGroups[i].SetActive(i < visibleCount);
    }

    public void Move(float distance)
    {
        transform.Translate(Vector3.back * distance, Space.World);
    }

    public bool IsPastDestroyPoint()
    {
        return transform.position.z <= destroyZ;
    }
}
