using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WireBootstrapObjects
{
    [MenuItem("Tools/Wire Bootstrap Objects")]
    public static void Wire()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("No active scene.");
            return;
        }

        int created = 0;
        created += EnsureObject<SubtitleManager>("[SubtitleManager]");
        created += EnsureObject<HorizonSmoke>("[HorizonSmoke]", new Vector3(0f, 25f, 150f));
        created += EnsureObject<DistantSteamerHorn>("[DistantSteamerHorn]");
        created += EnsureObject<AmbientSeagulls>("[AmbientSeagulls]");
        created += EnsureObject<DistantIronclads>("[DistantIronclads]");
        created += EnsureObject<OpeningLine>("[OpeningLine]");
        created += EnsureObject<ZoneSubtitles>("[ZoneSubtitles]");
        created += EnsureObject<PlayBoundary>("[PlayBoundary]");

        if (created > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Wire Bootstrap Objects: added {created} new object(s) and saved {scene.name}.");
        }
        else
        {
            Debug.Log("Wire Bootstrap Objects: nothing to add, all objects already present.");
        }
    }

    static int EnsureObject<T>(string goName, Vector3? position = null) where T : MonoBehaviour
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null) return 0;

        var go = new GameObject(goName);
        if (position.HasValue) go.transform.position = position.Value;
        go.AddComponent<T>();
        return 1;
    }
}
