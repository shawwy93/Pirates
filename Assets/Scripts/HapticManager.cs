using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HapticManager : MonoBehaviour
{
    static HapticManager instance;

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultGrabAmplitude  = 0.30f;
    [Range(0f, 1f)] public float defaultActivateAmp    = 0.55f;
    [Range(0f, 1f)] public float heavyAmp              = 0.85f;
    public float defaultGrabDuration      = 0.06f;
    public float defaultActivateDuration  = 0.12f;
    public float heavyDuration            = 0.22f;

    [Tooltip("Re-scan the scene for new interactables every few seconds so late spawns (e.g. key on crow's nest activation) also get hooked.")]
    public float rescanInterval = 2f;

    readonly System.Collections.Generic.HashSet<XRBaseInteractable> hooked =
        new System.Collections.Generic.HashSet<XRBaseInteractable>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureForActiveScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForActiveScene();

    static void EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();

        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        if (instance == null)
        {
            var go = new GameObject("[HapticManager]");
            instance = go.AddComponent<HapticManager>();
            DontDestroyOnLoad(go);
        }
        instance.HookEverything();
    }

    void Start()
    {
        StartCoroutine(PeriodicRescan());
    }

    IEnumerator PeriodicRescan()
    {
        while (true)
        {
            yield return new WaitForSeconds(rescanInterval);
            HookEverything();
        }
    }

    void HookEverything()
    {
        var all = FindObjectsByType<XRBaseInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ia in all) Hook(ia);
    }

    void Hook(XRBaseInteractable ia)
    {
        if (ia == null || hooked.Contains(ia)) return;
        hooked.Add(ia);

        ia.selectEntered.AddListener(OnSelectEntered);
        ia.activated.AddListener(OnActivated);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var (amp, dur) = ResolveIntensity(args.interactableObject as XRBaseInteractable, isActivate: false);
        Pulse(args.interactorObject, amp, dur);
    }

    void OnActivated(ActivateEventArgs args)
    {
        var (amp, dur) = ResolveIntensity(args.interactableObject as XRBaseInteractable, isActivate: true);
        Pulse(args.interactorObject, amp, dur);
    }

    (float amp, float dur) ResolveIntensity(XRBaseInteractable ia, bool isActivate)
    {
        if (ia != null)
        {
            var over = ia.GetComponent<HapticOverride>();
            if (over != null) return isActivate ? over.ActivateTuple() : over.GrabTuple();
        }

        string n = (ia != null ? ia.name : string.Empty).ToLowerInvariant();
        if (n.Contains("cannon") || n.Contains("fuse"))
            return (heavyAmp, heavyDuration);
        if (n.Contains("door") || n.Contains("hinge"))
            return (defaultActivateAmp, defaultActivateDuration);
        if (n.Contains("key"))
            return (0.45f, 0.09f);
        if (n.Contains("map"))
            return (0.22f, 0.05f);

        return isActivate
            ? (defaultActivateAmp, defaultActivateDuration)
            : (defaultGrabAmplitude, defaultGrabDuration);
    }

    public static void Pulse(IXRInteractor interactor, float amplitude, float duration)
    {
        if (interactor == null) return;
        amplitude = Mathf.Clamp01(amplitude);
        duration  = Mathf.Max(0f, duration);

        var mb = interactor as MonoBehaviour;
        if (mb == null) return;

        var input = mb.GetComponentInParent<XRBaseInputInteractor>();
        if (input != null)
        {
            try { input.SendHapticImpulse(amplitude, duration); } catch { }
        }
    }

    public static void PulseBoth(float amplitude, float duration)
    {
        var interactors = FindObjectsByType<XRBaseInputInteractor>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var i in interactors)
        {
            try { i.SendHapticImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0f, duration)); } catch { }
        }
    }
}

public class HapticOverride : MonoBehaviour
{
    [Range(0f, 1f)] public float grabAmplitude     = 0.35f;
    public float                 grabDuration      = 0.07f;
    [Range(0f, 1f)] public float activateAmplitude = 0.55f;
    public float                 activateDuration  = 0.12f;

    public (float, float) GrabTuple()     => (grabAmplitude,     grabDuration);
    public (float, float) ActivateTuple() => (activateAmplitude, activateDuration);
}
