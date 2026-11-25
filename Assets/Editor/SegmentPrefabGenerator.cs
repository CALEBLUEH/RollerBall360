using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SegmentPrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string GameplayScenePath = "Assets/Scenes/Endless.unity";
    private const float SegmentLength = 300f;

    private sealed class SegmentDesign
    {
        public readonly int Index;
        public readonly float Gap;
        public readonly float[] Angles;
        public readonly bool KeepExistingAngles;

        public SegmentDesign(int index, float gap, bool keepExistingAngles, params float[] angles)
        {
            Index = index;
            Gap = gap;
            Angles = angles;
            KeepExistingAngles = keepExistingAngles;
        }
    }

    private static readonly SegmentDesign[] Designs =
    {
        // Segment0 and Segment1 retain their existing obstacle shapes while
        // receiving clean, deterministic Z spacing.
        new(0, 2f, true),
        new(1, 5f, true),

        // Segment2 and the five new variants use different circular patterns.
        new(2, 8f, false, 0f, 45f, 0f, 45f, 90f, 135f, 90f, 135f),
        new(3, 3f, false, 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f),
        new(4, 4f, false, 0f, 180f, 45f, 225f, 90f, 270f, 135f, 315f),
        new(5, 6f, false, 0f, 90f, 0f, 90f, 45f, 135f, 45f, 135f),
        new(6, 7f, false, 315f, 270f, 225f, 180f, 135f, 90f, 45f, 0f),
        new(7, 10f, false, 0f, 0f, 90f, 90f, 180f, 180f, 270f, 270f),
    };

    [InitializeOnLoadMethod]
    private static void GenerateMissingVariantsAfterCompile()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(7)) == null)
            EditorApplication.delayCall += RebuildSegmentPrefabs;
    }

    [MenuItem("Tools/Endless Runner/Rebuild Segment Prefabs")]
    public static void RebuildSegmentPrefabs()
    {
        try
        {
            for (int i = 0; i < Designs.Length; i++)
            {
                SegmentDesign design = Designs[i];
                string sourcePath = design.Index < 3
                    ? GetPrefabPath(design.Index)
                    : GetPrefabPath(2);

                BuildPrefab(sourcePath, GetPrefabPath(design.Index), design);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WirePrefabsIntoGameplayScene();

            Debug.Log(
                "Rebuilt Segment0-Segment7, normalized 300-unit connectors, " +
                "and assigned all eight prefabs to PipeManager."
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    private static void BuildPrefab(string sourcePath, string destinationPath, SegmentDesign design)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(sourcePath);

        try
        {
            prefabRoot.name = $"Segment{design.Index}";

            PipeSegment segment = prefabRoot.GetComponent<PipeSegment>();
            if (segment == null)
                throw new InvalidOperationException($"{sourcePath} has no PipeSegment component.");

            Transform spawnPoint = prefabRoot.transform.Find("SpawnPoint");
            if (spawnPoint == null)
                throw new InvalidOperationException($"{sourcePath} has no root-level SpawnPoint.");

            spawnPoint.localPosition = new Vector3(0f, 0f, SegmentLength);
            spawnPoint.localRotation = Quaternion.identity;
            segment.spawnPoint = spawnPoint;
            segment.destroyZ = -SegmentLength;
            segment.obstacleSpacing = design.Gap;

            List<Transform> obstacles = prefabRoot.transform
                .Cast<Transform>()
                .Where(IsObstacleContainer)
                .OrderBy(transform => transform.localPosition.z)
                .ToList();

            if (obstacles.Count == 0)
                throw new InvalidOperationException($"{sourcePath} has no root-level obstacle containers.");

            float firstZ = -0.5f * design.Gap * (obstacles.Count - 1);

            for (int i = 0; i < obstacles.Count; i++)
            {
                Transform obstacle = obstacles[i];
                Vector3 position = obstacle.localPosition;
                position.z = firstZ + design.Gap * i;
                obstacle.localPosition = position;

                if (!design.KeepExistingAngles && design.Angles.Length > 0)
                {
                    float angle = design.Angles[i % design.Angles.Length];
                    obstacle.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, destinationPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool IsObstacleContainer(Transform child)
    {
        return child.name.StartsWith("ObstacleGroup", StringComparison.Ordinal) ||
               child.name.StartsWith("ObstacleArrangement", StringComparison.Ordinal);
    }

    private static string GetPrefabPath(int index)
    {
        return $"{PrefabFolder}/Segment{index}.prefab";
    }

    private static void WirePrefabsIntoGameplayScene()
    {
        Scene scene = SceneManager.GetSceneByPath(GameplayScenePath);
        bool closeWhenFinished = !scene.IsValid() || !scene.isLoaded;

        if (closeWhenFinished)
            scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

        try
        {
            PipeManager manager = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PipeManager>(true))
                .FirstOrDefault();

            if (manager == null)
                throw new InvalidOperationException("Endless.unity has no PipeManager component.");

            manager.pipePrefabs = Designs
                .Select(design => AssetDatabase.LoadAssetAtPath<PipeSegment>(GetPrefabPath(design.Index)))
                .ToArray();
            manager.startingSegments = 3;
            manager.spawnAheadZ = 600f;
            manager.startingMoveSpeed = 30f;
            manager.accelerationPerSecond = 0.75f;
            manager.maximumMoveSpeed = 70f;

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Unity could not save Endless.unity.");
        }
        finally
        {
            if (closeWhenFinished && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
