using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class AddTeleportAreas
{
    static readonly string[] Patterns = {
        "beach", "cliff", "deck", "wood floor", "ground", "sand",
        "rock", "path", "stairs", "ramp", "dock", "jetty"
    };

    [MenuItem("Tools/Add Teleport Areas To Walkable Surfaces")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        int added = 0, skipped = 0;
        var matched = new List<Transform>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.gameObject.scene != scene) continue;
            if (t.gameObject.hideFlags != HideFlags.None) continue;

            var n = t.name.ToLowerInvariant();
            bool match = false;
            foreach (var p in Patterns) { if (n.Contains(p)) { match = true; break; } }
            if (!match) continue;
            matched.Add(t);
        }

        foreach (var t in matched)
        {
            foreach (var mesh in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = mesh.gameObject;
                if (go.GetComponent<TeleportationArea>() != null) { skipped++; continue; }

                var col = go.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.convex = false;
                    col = mc;
                }
                if (col.isTrigger) col.isTrigger = false;

                go.AddComponent<TeleportationArea>();
                added++;
            }
        }

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"Teleport Areas: added {added}, skipped {skipped} (already had component). Scene saved.");
    }
}
