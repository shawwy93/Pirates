using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneSubtitles : MonoBehaviour
{
    static ZoneSubtitles instance;

    struct Zone
    {
        public string  targetName;
        public float   radius;
        public string  line;
        public float   duration;
        public Transform cached;
        public bool    fired;
    }

    List<Zone> zones = new List<Zone>();
    Transform  cam;
    float      rescanTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (_, __) => EnsureForActiveScene();
        EnsureForActiveScene();
    }

    static void EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name)) return;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        if (instance != null) { instance.ResetZones(); return; }

        var existing = FindAnyObjectByType<ZoneSubtitles>();
        if (existing != null) { instance = existing; instance.ResetZones(); return; }

        var go = new GameObject("[ZoneSubtitles]");
        instance = go.AddComponent<ZoneSubtitles>();
    }

    void Awake()
    {
        ResetZones();
    }

    void ResetZones()
    {
        zones.Clear();
        zones.Add(new Zone {
            targetName = "CrowsNest",
            radius     = 6f,
            line       = "[the wind's still ours up here]",
            duration   = 3.5f
        });
        zones.Add(new Zone {
            targetName = "Cannon_Quest",
            radius     = 4f,
            line       = "[the wick's dry. pull hard]",
            duration   = 3.2f
        });
    }

    void Update()
    {
        RefreshCamera();
        if (cam == null) return;

        rescanTimer -= Time.deltaTime;
        bool rescan  = rescanTimer <= 0f;
        if (rescan) rescanTimer = 2f;

        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (z.fired) continue;

            if (z.cached == null || rescan)
                z.cached = FindByName(z.targetName);

            if (z.cached == null) { zones[i] = z; continue; }

            float sqr = (z.cached.position - cam.position).sqrMagnitude;
            if (sqr <= z.radius * z.radius)
            {
                SubtitleManager.Show(z.line, z.duration);
                z.fired = true;
            }

            zones[i] = z;
        }
    }

    void RefreshCamera()
    {
        if (cam != null && cam.gameObject.activeInHierarchy) return;
        var c = Camera.main;
        if (c != null) { cam = c.transform; return; }
        foreach (var any in Resources.FindObjectsOfTypeAll<Camera>())
            if (any != null && any.enabled && any.gameObject.activeInHierarchy) { cam = any.transform; return; }
    }

    static Transform FindByName(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (t.gameObject.scene != scene) continue;
            if (t.gameObject.hideFlags != HideFlags.None) continue;
            if (t.name == name) return t;
        }
        return null;
    }
}
