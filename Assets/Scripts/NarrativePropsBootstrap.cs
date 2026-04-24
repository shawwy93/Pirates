using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class NarrativePropsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (_, __) => WireCurrentScene();
        WireCurrentScene();
    }

    static void WireCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name)) return;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        var maps = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.gameObject.scene != scene) continue;
            var n = t.name.ToLowerInvariant();
            if (!n.Contains("sm_item_map") && !n.Contains("map_03")) continue;
            maps.Add(t.gameObject);
        }

        if (maps.Count == 0) return;

        var candidates = new List<GameObject>();
        foreach (var m in maps)
        {
            if (m.GetComponent<MapRiddleDisplay>() != null) continue;
            if (m.GetComponent<CaptainsLetterDisplay>() != null) continue;
            if (m.GetComponent<LogbookMapDisplay>() != null) continue;
            if (m.GetComponent<DesertersNoteDisplay>() != null) continue;
            candidates.Add(m);
        }

        candidates.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        bool hasLetter = false, hasLog = false, hasDeserter = false;
        foreach (var m in maps)
        {
            if (m.GetComponent<CaptainsLetterDisplay>() != null) hasLetter   = true;
            if (m.GetComponent<LogbookMapDisplay>() != null)     hasLog      = true;
            if (m.GetComponent<DesertersNoteDisplay>() != null)  hasDeserter = true;
        }

        if (!hasLetter && candidates.Count > 0)
        {
            var host = candidates[0];
            candidates.RemoveAt(0);
            EnsureGrabbable(host);
            host.AddComponent<CaptainsLetterDisplay>();
            AddGlow(host);
            Debug.Log($"[NarrativePropsBootstrap] letter -> {host.name} at {host.transform.position}");
        }

        if (!hasLog && candidates.Count > 0)
        {
            var host = candidates[0];
            candidates.RemoveAt(0);
            EnsureGrabbable(host);
            host.AddComponent<LogbookMapDisplay>();
            AddGlow(host);
            Debug.Log($"[NarrativePropsBootstrap] logbook -> {host.name} at {host.transform.position}");
        }

        if (!hasDeserter && candidates.Count > 0)
        {
            var host = candidates[0];
            candidates.RemoveAt(0);
            EnsureGrabbable(host);
            host.AddComponent<DesertersNoteDisplay>();
            AddGlow(host);
            Debug.Log($"[NarrativePropsBootstrap] deserter -> {host.name} at {host.transform.position}");
        }
    }

    static void AddGlow(GameObject go)
    {
        if (go.GetComponent<ReadableGlow>() == null)
            go.AddComponent<ReadableGlow>();
    }

    static void EnsureGrabbable(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.useGravity  = false;
            rb.isKinematic = true;
        }
        if (go.GetComponent<XRGrabInteractable>() == null)
            go.AddComponent<XRGrabInteractable>();
    }
}
