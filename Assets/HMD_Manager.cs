using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class HMD_Manager : MonoBehaviour
{
    [SerializeField] GameObject xrPlayer;
    [SerializeField] GameObject fpsPlayer;
    [SerializeField] float xrStartupWait = 2f;

    bool? usingXRPlayer;
    readonly List<GameObject> xrPlayerCandidates = new List<GameObject>();
    readonly List<GameObject> fpsPlayerCandidates = new List<GameObject>();

    void Start()
    {
        FindPlayersIfMissing();
        StartCoroutine(ChoosePlayerWhenXRIsReady());
    }

    void Update()
    {
        FindPlayersIfMissing();

        if (!usingXRPlayer.HasValue)
        {
            return;
        }

        ApplyPlayerMode(IsXRActive());
    }

    IEnumerator ChoosePlayerWhenXRIsReady()
    {
        var deadline = Time.realtimeSinceStartup + xrStartupWait;
        while (Time.realtimeSinceStartup < deadline && !IsXRActive())
        {
            yield return null;
        }

        ApplyPlayerMode(IsXRActive(), true);
    }

    bool IsXRActive()
    {
        var displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displaySubsystems);

        foreach (var subsystem in displaySubsystems)
        {
            if (subsystem.running)
            {
                return true;
            }
        }

        return false;
    }

    void ApplyPlayerMode(bool useXR, bool logChange = false)
    {
        if (xrPlayer == null || fpsPlayer == null)
        {
            Debug.LogWarning("HMD Manager needs both XR Player and FPS Player assigned.");
            return;
        }

        if (usingXRPlayer.HasValue && usingXRPlayer.Value == useXR)
        {
            EnforceExclusivePlayers(useXR);
            return;
        }

        usingXRPlayer = useXR;
        EnforceExclusivePlayers(useXR);

        if (logChange)
        {
            Debug.Log(useXR ? "Using XR Player with HMD" : "No HMD detected, using FPS Player");
        }
    }

    void EnforceExclusivePlayers(bool useXR)
    {
        SetOnlyPreferredActive(xrPlayerCandidates, xrPlayer, useXR);
        SetOnlyPreferredActive(fpsPlayerCandidates, fpsPlayer, !useXR);
    }

    void FindPlayersIfMissing()
    {
        xrPlayerCandidates.Clear();
        fpsPlayerCandidates.Clear();

        AddUnique(xrPlayerCandidates, xrPlayer);
        AddUnique(fpsPlayerCandidates, fpsPlayer);

        var scene = gameObject.scene;
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var candidate in allObjects)
        {
            if (candidate == null || candidate.scene != scene || candidate.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (candidate.name.StartsWith("XR PLAYER"))
            {
                AddUnique(xrPlayerCandidates, candidate);
            }
            else if (candidate.name.StartsWith("Player_FPS"))
            {
                AddUnique(fpsPlayerCandidates, candidate);
            }
        }

        if (xrPlayer == null)
        {
            xrPlayer = xrPlayerCandidates.Count > 0 ? xrPlayerCandidates[0] : null;
        }

        if (fpsPlayer == null)
        {
            fpsPlayer = fpsPlayerCandidates.Count > 0 ? fpsPlayerCandidates[0] : null;
        }
    }

    static void AddUnique(List<GameObject> objects, GameObject candidate)
    {
        if (candidate != null && !objects.Contains(candidate))
        {
            objects.Add(candidate);
        }
    }

    static void SetOnlyPreferredActive(List<GameObject> candidates, GameObject preferred, bool active)
    {
        foreach (var candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            var shouldBeActive = active && candidate == preferred;
            if (candidate.activeSelf != shouldBeActive)
            {
                candidate.SetActive(shouldBeActive);
            }
        }
    }
}
