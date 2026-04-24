using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ReadableGlow : MonoBehaviour
{
    Light glow;
    float phase;
    float baseIntensity = 0.6f;
    float pulseAmp      = 0.25f;
    float pulseSpeed    = 1.4f;
    XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        var go = new GameObject("ReadableGlow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, -0.05f, 0f);

        glow           = go.AddComponent<Light>();
        glow.type      = LightType.Point;
        glow.color     = new Color(1f, 0.82f, 0.45f);
        glow.intensity = baseIntensity;
        glow.range     = 1.2f;
        glow.shadows   = LightShadows.None;
    }

    void OnEnable()
    {
        if (grab != null) grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDisable()
    {
        if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void Update()
    {
        if (glow == null) return;
        phase += Time.deltaTime * pulseSpeed;
        glow.intensity = baseIntensity + Mathf.Sin(phase) * pulseAmp;
    }

    void OnGrabbed(SelectEnterEventArgs _)
    {
        if (glow != null) Destroy(glow.gameObject);
        Destroy(this);
    }
}
