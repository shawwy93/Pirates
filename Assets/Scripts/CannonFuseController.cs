using System.Collections;
using System.Collections.Generic;
using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CannonFuseController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("LaunchProjectile on the cannon parent — drag the cannon GameObject here.")]
    [SerializeField] LaunchProjectile launcher;

    [Tooltip("FX_Cannon_Smoke prefab from PolygonPirates/FX.")]
    [SerializeField] GameObject cannonSmokePrefab;

    [Tooltip("Where the smoke spawns — create a child empty 'MuzzlePoint' at the cannon barrel end.")]
    [SerializeField] Transform muzzlePoint;

    [Header("Pull Settings")]
    [Tooltip("How far the player must pull the fuse downward to trigger firing.")]
    [SerializeField] float pullThreshold = 0.28f;

    [Tooltip("Downward pull is measured along this axis in world space. Leave as (0,-1,0).")]
    [SerializeField] Vector3 pullAxis = Vector3.down;

    [Header("Fuse Glow")]
    [SerializeField] Renderer fuseRenderer;
    [SerializeField] Color    fuseIdleColor   = new Color(0.6f, 0.4f, 0.1f);
    [SerializeField] Color    fuseActiveColor = new Color(1f, 0.5f, 0f);
    [SerializeField] float    fuseGlowMult    = 4f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip   fireClip;

    XRGrabInteractable grab;

    Vector3 grabWorldStart;
    bool    isBeingPulled;
    bool    hasFired;

    UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor activeInteractor;
    AudioClip synthBoom;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void Start()
    {
        synthBoom = BuildCannonBoom();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        if (fuseRenderer != null)
        {
            var mat = fuseRenderer.material;
            mat.color = fuseIdleColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", fuseIdleColor * 0.5f);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hasFired) return;

        activeInteractor = args.interactorObject;
        grabWorldStart   = args.interactorObject.GetAttachTransform(grab).position;
        isBeingPulled    = true;

        SetFuseGlow(fuseActiveColor, fuseGlowMult);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isBeingPulled    = false;
        activeInteractor = null;

        if (!hasFired) SetFuseGlow(fuseIdleColor, 0.5f);
    }

    void Update()
    {
        if (!isBeingPulled || hasFired || activeInteractor == null) return;

        Vector3 handNow    = activeInteractor.GetAttachTransform(grab).position;
        float   pullAmount = Vector3.Dot(handNow - grabWorldStart, pullAxis.normalized);

        if (pullAmount >= pullThreshold)
            Fire();
    }

    void Fire()
    {
        hasFired      = true;
        isBeingPulled = false;

        grab.interactionManager?.CancelInteractableSelection((IXRSelectInteractable)grab);

        launcher?.Fire();

        var spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position;
        var spawnRot = muzzlePoint != null ? muzzlePoint.rotation : Quaternion.identity;
        if (cannonSmokePrefab != null)
            Instantiate(cannonSmokePrefab, spawnPos, spawnRot);

        MuzzleBurst.Spawn(spawnPos, spawnRot);

        var clip = fireClip != null ? fireClip : synthBoom;
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);

        SendHaptics(1f, 0.4f);

        SetFuseGlow(Color.white, 8f);
        StartCoroutine(DeactivateAfterDelay(0.15f));

        PirateObjectiveController.NotifySignalFired();
    }

    IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    void SetFuseGlow(Color color, float intensity)
    {
        if (fuseRenderer == null) return;
        var mat = fuseRenderer.material;
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
    }

    static void SendHaptics(float amplitude, float duration)
    {
        try
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand,  devices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, devices);
            foreach (var d in devices) d.SendHapticImpulse(0, amplitude, duration);
        }
        catch {  }
    }

    static AudioClip BuildCannonBoom()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 0.8f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t      = i / (float)rate;
            float decay  = Mathf.Exp(-t * 5f);
            float attack = Mathf.Min(1f, t / 0.01f);
            samples[i]   = (
                Mathf.Sin(2f * Mathf.PI * 80f  * t) * 0.6f +
                Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.25f +
                (Random.value * 2f - 1f)             * 0.15f
            ) * decay * attack;
        }

        var clip = AudioClip.Create("CannonBoom", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
