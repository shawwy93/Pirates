using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SolarSocketController : MonoBehaviour
{
    [Header("Sockets — drag in 3 XRSocketInteractors")]
    [SerializeField] List<XRSocketInteractor> sockets = new List<XRSocketInteractor>();

    [Header("Socket Visual Markers — one Renderer per socket (the glow disc)")]
    [SerializeField] List<Renderer> socketMarkers = new List<Renderer>();

    [Header("Colours")]
    [SerializeField] Color emptyColor  = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] Color filledColor = new Color(1f, 0.85f, 0.05f);
    [SerializeField] float glowIntensity = 3.5f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip   placementClip;

    AudioClip synthPlacementClip;

    void Start()
    {

        synthPlacementClip = BuildPlacementTone();

        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] == null) continue;

            SetMarkerState(i, false);

            int captured = i;
            sockets[i].selectEntered.AddListener(_ => OnCellPlaced(captured));
            sockets[i].selectExited.AddListener(_ =>  OnCellRemoved(captured));
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnDestroy()
    {
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] == null) continue;
            sockets[i].selectEntered.RemoveAllListeners();
            sockets[i].selectExited.RemoveAllListeners();
        }
    }

    void OnCellPlaced(int index)
    {
        SetMarkerState(index, true);
        PlayPlacementSound();
        PirateObjectiveController.NotifySolarCellPlaced();
    }

    void OnCellRemoved(int index)
    {

        SetMarkerState(index, false);
    }

    void SetMarkerState(int index, bool filled)
    {
        if (index >= socketMarkers.Count || socketMarkers[index] == null) return;

        var mat   = socketMarkers[index].material;
        var color = filled ? filledColor : emptyColor;

        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * (filled ? glowIntensity : 0f));
    }

    void PlayPlacementSound()
    {
        var clip = placementClip != null ? placementClip : synthPlacementClip;
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    static AudioClip BuildPlacementTone()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 0.35f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];
        int   half     = count / 2;

        for (int i = 0; i < count; i++)
        {
            float t    = i / (float)rate;
            float fade = 1f - i / (float)count;

            float freq  = i < half ? 660f : 880f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * fade;
        }

        var clip = AudioClip.Create("SolarPlacement", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
