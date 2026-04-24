using Unity.VRTemplate;
using UnityEngine;

public class WindWheelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] XRKnob knob;

    [SerializeField] Transform windIndicatorArrow;

    [SerializeField] Renderer wheelRenderer;

    [Header("Alignment Window")]
    [Range(0f, 1f)]
    [SerializeField] float targetValue    = 0.5f;
    [SerializeField] float tolerance      = 0.12f;
    [SerializeField] float sustainSeconds = 1.5f;

    [Header("Glow Colours")]
    [SerializeField] Color idleEmission    = Color.black;
    [SerializeField] Color alignedEmission = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] float alignedGlowMult = 3f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip   lockClip;

    float currentKnobValue = 0.5f;
    float timeInWindow;
    bool  hasConfirmed;

    Material wheelMat;
    AudioClip synthLockClip;

    void Start()
    {
        if (knob == null) knob = GetComponent<XRKnob>();

        if (knob != null)
        {

            currentKnobValue = knob.value;
            knob.onValueChange.AddListener(OnKnobValueChanged);
        }
        else
        {
            Debug.LogWarning("[WindWheel] No XRKnob found — wheel alignment will not work.");
        }

        if (wheelRenderer != null)
        {
            wheelMat = wheelRenderer.material;
            SetGlow(idleEmission, 0f);
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        synthLockClip = BuildLockTone();
    }

    void OnDestroy()
    {
        if (knob != null) knob.onValueChange.RemoveListener(OnKnobValueChanged);
    }

    void OnKnobValueChanged(float v) => currentKnobValue = v;

    void Update()
    {
        if (hasConfirmed || knob == null) return;

        RotateWindIndicator();

        bool inWindow = Mathf.Abs(currentKnobValue - targetValue) <= tolerance;

        if (inWindow)
        {
            timeInWindow += Time.deltaTime;

            float progress = Mathf.Clamp01(timeInWindow / sustainSeconds);
            SetGlow(Color.Lerp(idleEmission, alignedEmission, progress), alignedGlowMult * progress);

            if (timeInWindow >= sustainSeconds)
                ConfirmAlignment();
        }
        else
        {

            timeInWindow = Mathf.MoveTowards(timeInWindow, 0f, Time.deltaTime * 2f);
            float progress = Mathf.Clamp01(timeInWindow / sustainSeconds);
            SetGlow(Color.Lerp(idleEmission, alignedEmission, progress), alignedGlowMult * progress);
        }
    }

    void ConfirmAlignment()
    {
        hasConfirmed = true;

        if (knob != null) knob.enabled = false;

        SetGlow(alignedEmission, alignedGlowMult);

        var clip = lockClip != null ? lockClip : synthLockClip;
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);

        SendHaptics(0.7f, 0.25f);

        PirateObjectiveController.NotifyWindAligned();
    }

    void RotateWindIndicator()
    {
        if (windIndicatorArrow == null || knob == null) return;
        float targetAngle  = Mathf.Lerp(knob.minAngle, knob.maxAngle, targetValue);
        float currentAngle = Mathf.Lerp(knob.minAngle, knob.maxAngle, currentKnobValue);
        float delta        = Mathf.DeltaAngle(currentAngle, targetAngle);
        windIndicatorArrow.localEulerAngles = new Vector3(0f, -delta, 0f);
    }

    void SetGlow(Color color, float intensity)
    {
        if (wheelMat == null) return;
        wheelMat.EnableKeyword("_EMISSION");
        wheelMat.SetColor("_EmissionColor", color * intensity);
    }

    static void SendHaptics(float amplitude, float duration)
    {
        try
        {
            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand,  devices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, devices);
            foreach (var d in devices) d.SendHapticImpulse(0, amplitude, duration);
        }
        catch {  }
    }

    static AudioClip BuildLockTone()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 0.5f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];

        float[] freqs = { 440f, 550f, 660f, 880f };
        int     step  = count / freqs.Length;

        for (int i = 0; i < count; i++)
        {
            float t    = i / (float)rate;
            float fade = 1f - i / (float)count;
            float freq = freqs[Mathf.Min(i / step, freqs.Length - 1)];
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * fade;
        }

        var clip = AudioClip.Create("WindLock", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
