using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayBoundary : MonoBehaviour
{
    static PlayBoundary instance;

    [Tooltip("Any GameObject whose name contains one of these strings is off-limits.")]
    public List<string> blockedNames = new List<string> { "Ocean" };

    [Tooltip("How far above the camera to start the downward raycast.")]
    public float rayStartAbove = 3f;

    [Tooltip("How far down to cast.")]
    public float rayDistance = 3000f;

    Camera    hmd;
    Transform xrOrigin;
    Vector3   lastSafeRigPos;
    bool      hasSafe;

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

        if (instance != null) { instance.Rebuild(); return; }
        var existing = FindAnyObjectByType<PlayBoundary>();
        if (existing != null) { instance = existing; instance.Rebuild(); return; }

        var go = new GameObject("[PlayBoundary]");
        instance = go.AddComponent<PlayBoundary>();
    }

    void Start() => Rebuild();

    void Rebuild()
    {
        hasSafe = false;
        var blocked = FindBlocked();
        foreach (var t in blocked)
        {
            KillTeleportOn(t);
            EnsureRaycastableCollider(t);
        }
        Debug.Log($"[PlayBoundary] armed. blocked objects found: {blocked.Count}");
    }

    List<Transform> FindBlocked()
    {
        var list = new List<Transform>();
        var scene = SceneManager.GetActiveScene();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (t.gameObject.scene != scene) continue;
            if (t.gameObject.hideFlags != HideFlags.None) continue;
            if (MatchesBlockedName(t)) list.Add(t);
        }
        return list;
    }

    bool MatchesBlockedName(Transform t)
    {
        if (t == null) return false;
        foreach (var s in blockedNames)
        {
            if (string.IsNullOrEmpty(s)) continue;

            if (t.name == s) return true;
        }
        return false;
    }

    bool IsUnderBlocked(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
            if (MatchesBlockedName(cur)) return true;
        return false;
    }

    void KillTeleportOn(Transform t)
    {
        foreach (var b in t.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b == null) continue;
            var tn = b.GetType().Name;
            if (tn == "TeleportationArea" || tn == "TeleportationAnchor" || tn == "TeleportationMultiAnchorVolume")
                b.enabled = false;
        }
    }

    void EnsureRaycastableCollider(Transform t)
    {

        var cols = t.GetComponentsInChildren<Collider>(true);
        if (cols != null && cols.Length > 0) return;

        var slab = new GameObject(t.name + "_RayTarget");
        slab.transform.SetParent(t, false);
        slab.transform.localPosition = Vector3.zero;

        var box = slab.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size      = new Vector3(4000f, 0.2f, 4000f);
        box.center    = new Vector3(0f, 0f, 0f);
    }

    void LateUpdate()
    {
        RefreshCamera();
        if (hmd == null) return;
        if (xrOrigin == null || !xrOrigin.gameObject.activeInHierarchy)
            xrOrigin = FindXrOrigin(hmd);
        if (xrOrigin == null) return;

        var camP   = hmd.transform.position;
        var origin = new Vector3(camP.x, camP.y + rayStartAbove, camP.z);

        bool overBlocked = false;
        var hits = Physics.RaycastAll(origin, Vector3.down, rayDistance, ~0, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider == null) continue;

                if (h.collider.transform.IsChildOf(xrOrigin)) continue;

                overBlocked = IsUnderBlocked(h.collider.transform);
                break;
            }
        }

        if (overBlocked)
        {
            if (hasSafe) SetRigPosition(xrOrigin, lastSafeRigPos);
        }
        else
        {
            lastSafeRigPos = xrOrigin.position;
            hasSafe        = true;
        }
    }

    static void SetRigPosition(Transform rig, Vector3 pos)
    {
        var cc = rig.GetComponentInChildren<CharacterController>(true);
        if (cc != null)
        {
            bool wasEnabled = cc.enabled;
            cc.enabled = false;
            rig.position = pos;
            cc.enabled = wasEnabled;
        }
        else
        {
            rig.position = pos;
        }
    }

    void RefreshCamera()
    {
        if (hmd != null && hmd.enabled && hmd.gameObject.activeInHierarchy) return;
        hmd = Camera.main;
        if (hmd != null && hmd.enabled && hmd.gameObject.activeInHierarchy) return;
        foreach (var any in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (any == null) continue;
            if (!any.enabled) continue;
            if (!any.gameObject.activeInHierarchy) continue;
            if (any.gameObject.hideFlags != HideFlags.None) continue;
            hmd = any;
            return;
        }
    }

    static Transform FindXrOrigin(Camera cam)
    {
        var scene = SceneManager.GetActiveScene();

        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (t.gameObject.scene != scene) continue;
            if (t.gameObject.hideFlags != HideFlags.None) continue;
            var n = t.name;
            if (n.Contains("XR Origin") || n.Contains("XR PLAYER") || n.Contains("XR Rig") ||
                n.Contains("XRRig")     || n.Contains("Player_FPS"))
                return t;
        }

        if (cam != null)
        {
            for (var t = cam.transform; t != null; t = t.parent)
                if (t.GetComponent<CharacterController>() != null) return t;
            return cam.transform.root;
        }
        return null;
    }
}
