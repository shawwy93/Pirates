using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PirateObjectiveController : MonoBehaviour
{
    static PirateObjectiveController instance;

    public enum ObjectiveState
    {
        ReadMap,
        UnlockDoor,
        FireSignalCannon,
        Won,
        Failed
    }

    [Header("HUD")]
    [SerializeField] bool showObjectiveHud = true;

    Transform escapeShip;
    Transform cameraRig;

    ObjectiveState state = ObjectiveState.ReadMap;
    bool           hasSceneObjects;
    bool           winSequenceStarted;
    bool           failSequenceStarted;

    AudioSource audioSource;
    AudioSource swellSource;
    AudioClip   collectClip;
    AudioClip   successClip;
    AudioClip   winSwellClip;
    AudioClip   failDroneClip;

    Canvas          objectiveCanvas;
    TextMeshProUGUI objectiveText;
    Image           hudBackground;

    Canvas          cinematicCanvas;
    TextMeshProUGUI cinematicText;
    Image           cinematicVignette;

    List<Renderer> sailRenderers = new List<Renderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var scene = SceneManager.GetActiveScene();
        if (IsMenuScene(scene.name))
        {

            SceneManager.sceneLoaded += BootstrapOnGameSceneLoaded;
            return;
        }
        EnsureInstance();
    }

    static void BootstrapOnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name)) return;
        SceneManager.sceneLoaded -= BootstrapOnGameSceneLoaded;
        EnsureInstance();
    }

    static bool IsMenuScene(string sceneName)
        => !string.IsNullOrEmpty(sceneName) && sceneName.ToLowerInvariant().Contains("mainmenu");

    static PirateObjectiveController EnsureInstance()
    {
        if (instance != null) return instance;
        var go = new GameObject("Pirates Plight Controller");
        instance = go.AddComponent<PirateObjectiveController>();
        DontDestroyOnLoad(go);
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    bool _started;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (_started) ResetAll(); }
    void Start() { _started = true; ResetAll(); }

    public static void NotifyMapRead()
    {
        if (instance == null || instance.state != ObjectiveState.ReadMap) return;
        instance.Play(instance.collectClip);
        SubtitleManager.Show("[parchment unfurls]  \"Where the crow keeps watch...\"", 3.5f);
        instance.AdvanceTo(ObjectiveState.UnlockDoor);
    }

    public static void NotifyDoorUnlocked()
    {
        if (instance == null || instance.state != ObjectiveState.UnlockDoor) return;
        instance.Play(instance.successClip);
        SubtitleManager.Show("[lock clicks open]", 2.2f);
        instance.AdvanceTo(ObjectiveState.FireSignalCannon);
    }

    public static void NotifySignalFired()
    {
        if (instance == null || instance.state != ObjectiveState.FireSignalCannon) return;
        instance.Play(instance.successClip);
        SubtitleManager.Show("[cannon thunders]", 2.5f);
        instance.AdvanceTo(ObjectiveState.Won);
    }

    public static void NotifySolarCellPlaced() {  }
    public static void NotifyWindAligned()     {  }
    public static void NotifyTidalFired()      => NotifySignalFired();

    public static ObjectiveState CurrentState =>
        instance != null ? instance.state : ObjectiveState.ReadMap;

    void Update()
    {
        if (!hasSceneObjects) return;

        RefreshCamera();
        if (showObjectiveHud) PinHudToCamera();
        PinCinematicToCamera();
        HandleRestartInput();
    }

    void AdvanceTo(ObjectiveState next)
    {
        state = next;

        if (next == ObjectiveState.Won)
        {
            if (!winSequenceStarted) { winSequenceStarted = true; StartCoroutine(PlayWinSequence()); }
        }
        else if (next == ObjectiveState.Failed)
        {
            if (!failSequenceStarted) { failSequenceStarted = true; StartCoroutine(PlayFailSequence()); }
        }
        else
        {
            UpdateHud();
        }
    }

    IEnumerator PlayWinSequence()
    {

        if (objectiveCanvas != null) objectiveCanvas.gameObject.SetActive(false);

        DiscoverSails();
        SetSailColor(new Color(0.55f, 0.50f, 0.40f));

        StartCoroutine(WarmSkyDuringWin(6f));

        HorizonSmoke.Retreat();
        DistantIronclads.Retreat();

        if (swellSource != null && winSwellClip != null)
        {
            swellSource.clip   = winSwellClip;
            swellSource.loop   = true;
            swellSource.volume = 0f;
            swellSource.Play();
        }

        yield return new WaitForSeconds(0.6f);

        float swellDuration = 3f;
        float elapsed       = 0f;
        Color sailStart     = new Color(0.55f, 0.50f, 0.40f);
        Color sailEnd       = Color.white;

        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / swellDuration);

            SetSailColor(Color.Lerp(sailStart, sailEnd, t));

            if (swellSource != null) swellSource.volume = Mathf.Lerp(0f, 0.65f, t);

            yield return null;
        }

        SetSailColor(sailEnd);
        if (swellSource != null) swellSource.volume = 0.65f;

        yield return StartCoroutine(FadeCinematicVignette(0f, 0.55f, 1.4f));
        yield return StartCoroutine(FadeCinematicText(
            "The fleet answers.\n<size=70%>Sails to the wind, away from the smoke.</size>",
            0f, 1f, 2f));

        yield return new WaitForSeconds(4.5f);

        yield return StartCoroutine(FadeCinematicText(
            "The fleet answers.\n<size=70%>Sails to the wind, away from the smoke.</size>",
            1f, 0f, 1.2f));

        yield return StartCoroutine(FadeCinematicText(
            "<size=80%>Pirates' Plight</size>\n" +
            "<size=50%><color=#D8C48E>A VR experience by Michael Shaw</color></size>\n\n" +
            "<size=42%><color=#AAAAAA>CM3150 Immersive Technologies, 2026</color></size>\n\n" +
            "<size=32%><color=#C8B78A>Music: Dave4884, LittleRobotSoundFactory</color></size>\n" +
            "<size=32%><color=#C8B78A>Sound Effects: uniuniversal, greysound</color></size>\n" +
            "<size=32%><color=#C8B78A>Font: Pirata One (SIL OFL)</color></size>\n" +
            "<size=32%><color=#C8B78A>Pirate assets: Synty Studios</color></size>\n\n" +
            "<size=52%>Thanks for playing.</size>",
            0f, 1f, 2.6f));

        yield return StartCoroutine(FadeSwellVolume(
            swellSource, swellSource != null ? swellSource.volume : 0f, 0f, 4f));

        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(FadeCinematicText(
            "<size=80%>Pirates' Plight</size>\n" +
            "<size=50%><color=#D8C48E>A VR experience by Michael Shaw</color></size>\n\n" +
            "<size=45%><color=#AAAAAA>CM3150 Immersive Technologies, 2026</color></size>\n\n" +
            "<size=55%>Thanks for playing.</size>",
            1f, 0f, 1.5f));

        yield return StartCoroutine(FadeVignetteToBlack(1.2f));

        yield return new WaitForSeconds(0.4f);

        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator FadeVignetteToBlack(float duration)
    {
        if (cinematicVignette == null) yield break;

        Color startColor = cinematicVignette.color;
        Color endColor   = new Color(0f, 0f, 0f, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cinematicVignette.color = Color.Lerp(startColor, endColor,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        cinematicVignette.color = endColor;
    }

    IEnumerator PlayFailSequence()
    {
        if (objectiveCanvas != null) objectiveCanvas.gameObject.SetActive(false);

        if (swellSource != null && failDroneClip != null)
        {
            swellSource.clip   = failDroneClip;
            swellSource.loop   = true;
            swellSource.volume = 0f;
            swellSource.Play();
        }

        yield return new WaitForSeconds(0.3f);

        if (cinematicVignette != null)
            cinematicVignette.color = new Color(0.35f, 0f, 0f, 0f);

        yield return StartCoroutine(FadeCinematicVignette(0f, 0.70f, 2.5f));

        if (swellSource != null) swellSource.volume = 0.45f;

        yield return StartCoroutine(FadeCinematicText(
            "The tide turned.\n<size=70%>The old ways won.</size>", 0f, 1f, 2.2f));

        yield return new WaitForSeconds(3f);

        if (cinematicText != null)
        {
            cinematicText.text =
                "The tide turned.\n<size=70%>The old ways won.</size>" +
                "\n\n<size=50%><color=#AAAAAA>Press R to try again</color></size>";
        }
    }

    IEnumerator FadeCinematicVignette(float from, float to, float duration)
    {
        if (cinematicVignette == null) yield break;

        float elapsed = 0f;
        Color c       = cinematicVignette.color;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            c.a        = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            cinematicVignette.color = c;
            yield return null;
        }
        c.a = to;
        cinematicVignette.color = c;
    }

    IEnumerator FadeCinematicText(string message, float from, float to, float duration)
    {
        if (cinematicText == null) yield break;

        cinematicText.text  = message;
        cinematicText.alpha = from;
        cinematicText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            cinematicText.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        cinematicText.alpha = to;
    }

    IEnumerator WarmSkyDuringWin(float duration)
    {

        bool  hadFog       = RenderSettings.fog;
        Color fogStart     = RenderSettings.fogColor;
        float densityStart = RenderSettings.fogDensity;
        Color ambientStart = RenderSettings.ambientLight;

        Color fogEnd       = Color.Lerp(fogStart, new Color(0.95f, 0.86f, 0.68f), 0.75f);
        float densityEnd   = densityStart * 0.35f;
        Color ambientEnd   = Color.Lerp(ambientStart, new Color(1.00f, 0.92f, 0.78f), 0.60f);

        Material sky = RenderSettings.skybox;
        bool hasSkyTint     = sky != null && sky.HasProperty("_SkyTint");
        bool hasPanTint     = sky != null && sky.HasProperty("_Tint");
        bool hasExposure    = sky != null && sky.HasProperty("_Exposure");

        Color tintStart = hasSkyTint ? sky.GetColor("_SkyTint")
                       : hasPanTint ? sky.GetColor("_Tint")
                       : Color.white;
        Color tintEnd   = Color.Lerp(tintStart, new Color(1.00f, 0.88f, 0.70f), 0.55f);

        float expStart = hasExposure ? sky.GetFloat("_Exposure") : 1f;
        float expEnd   = Mathf.Min(expStart * 1.35f, 2.2f);

        RenderSettings.fog = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            RenderSettings.fogColor     = Color.Lerp(fogStart,     fogEnd,     t);
            RenderSettings.fogDensity   = Mathf.Lerp(densityStart, densityEnd, t);
            RenderSettings.ambientLight = Color.Lerp(ambientStart, ambientEnd, t);

            if (sky != null)
            {
                if (hasSkyTint)  sky.SetColor("_SkyTint", Color.Lerp(tintStart, tintEnd, t));
                if (hasPanTint)  sky.SetColor("_Tint",    Color.Lerp(tintStart, tintEnd, t));
                if (hasExposure) sky.SetFloat("_Exposure", Mathf.Lerp(expStart, expEnd, t));
            }

            yield return null;
        }

        RenderSettings.fogColor     = fogEnd;
        RenderSettings.fogDensity   = densityEnd;
        RenderSettings.ambientLight = ambientEnd;
        if (sky != null)
        {
            if (hasSkyTint)  sky.SetColor("_SkyTint", tintEnd);
            if (hasPanTint)  sky.SetColor("_Tint",    tintEnd);
            if (hasExposure) sky.SetFloat("_Exposure", expEnd);
        }
        if (!hadFog) RenderSettings.fog = false;
    }

    IEnumerator FadeSwellVolume(AudioSource src, float from, float to, float duration)
    {
        if (src == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            src.volume  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        src.volume = to;
        if (to <= 0f) src.Stop();
    }

    void DiscoverSails()
    {
        sailRenderers.Clear();
        if (escapeShip == null) return;

        foreach (var r in escapeShip.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.ToLowerInvariant().Contains("sail"))
                sailRenderers.Add(r);
        }

        if (sailRenderers.Count == 0)
        {
            var all = escapeShip.GetComponentsInChildren<Renderer>(true);
            if (all.Length == 0) return;

            float maxY = float.MinValue, minY = float.MaxValue;
            foreach (var r in all)
            {
                maxY = Mathf.Max(maxY, r.bounds.center.y);
                minY = Mathf.Min(minY, r.bounds.center.y);
            }
            float threshold = Mathf.Lerp(minY, maxY, 0.55f);
            foreach (var r in all)
                if (r.bounds.center.y >= threshold)
                    sailRenderers.Add(r);
        }
    }

    void SetSailColor(Color color)
    {
        foreach (var r in sailRenderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.2f);
            }
        }
    }

    void DiscoverSceneObjects()
    {
        escapeShip      = null;
        hasSceneObjects = false;

        var activeScene = SceneManager.GetActiveScene();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!IsActiveSceneObject(t, activeScene)) continue;
            var n = t.name.ToLowerInvariant();

            if (escapeShip == null && (n.Contains("warship") || n.Contains("boat_large") || n.Contains("escape_ship")))
                escapeShip = t;
        }

        hasSceneObjects = true;
        Debug.Log($"[Pirates Plight] escape ship = {escapeShip?.name ?? "none"}");
    }

    static bool IsActiveSceneObject(Transform t, Scene scene)
        => t != null
           && t.gameObject.scene == scene
           && t.gameObject.hideFlags == HideFlags.None
           && t.gameObject.activeInHierarchy;

    void UpdateHud()
    {
        if (!showObjectiveHud || objectiveText == null) return;

        objectiveText.text = state switch
        {
            ObjectiveState.ReadMap          => "<b>Find and read the captain's map</b>",
            ObjectiveState.UnlockDoor       => "<b>Find the key and unlock the door</b>\n<size=80%>The riddle points the way</size>",
            ObjectiveState.FireSignalCannon => "<b>Fire the signal cannon</b>\n<size=80%>Grab the lanyard and pull hard</size>",
            _                               => string.Empty
        };
    }

    void PinHudToCamera()
    {
        if (objectiveCanvas == null || cameraRig == null) return;
        var t = objectiveCanvas.transform;
        if (t.parent != cameraRig) t.SetParent(cameraRig, false);
        t.localPosition = new Vector3(-0.52f, 0.30f, 1.20f);
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.0012f;
    }

    void PinCinematicToCamera()
    {
        if (cinematicCanvas == null || cameraRig == null) return;
        var t = cinematicCanvas.transform;
        if (t.parent != cameraRig) t.SetParent(cameraRig, false);
        t.localPosition = new Vector3(0f, 0f, 1.0f);
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.001f;
    }

    void BuildHud()
    {
        var canvasGO = new GameObject("PP Objective HUD");
        canvasGO.transform.SetParent(transform, false);

        objectiveCanvas = canvasGO.AddComponent<Canvas>();
        objectiveCanvas.renderMode   = RenderMode.WorldSpace;
        objectiveCanvas.sortingOrder = 200;
        objectiveCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 140f);

        var bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        hudBackground = bgGO.AddComponent<Image>();
        hudBackground.color = new Color(0f, 0.05f, 0f, 0.55f);
        var bgRect = hudBackground.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var textGO       = new GameObject("Objective Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        objectiveText    = textGO.AddComponent<TextMeshProUGUI>();
        objectiveText.fontSize        = 26;
        objectiveText.color           = new Color(1f, 0.92f, 0.6f);
        objectiveText.alignment       = TextAlignmentOptions.MidlineLeft;
        objectiveText.textWrappingMode = TextWrappingModes.Normal;
        var textRect = objectiveText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 10f);
        textRect.offsetMax = new Vector2(-16f, -10f);
    }

    void BuildCinematicCanvas()
    {
        var go = new GameObject("Cinematic Canvas");
        go.transform.SetParent(transform, false);

        cinematicCanvas = go.AddComponent<Canvas>();
        cinematicCanvas.renderMode   = RenderMode.WorldSpace;
        cinematicCanvas.sortingOrder = 300;
        var rt = cinematicCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000f, 600f);

        var vigGO = new GameObject("Vignette");
        vigGO.transform.SetParent(go.transform, false);
        cinematicVignette = vigGO.AddComponent<Image>();
        cinematicVignette.color = new Color(0f, 0.08f, 0f, 0f);
        var vigRect = cinematicVignette.rectTransform;
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.offsetMin = vigRect.offsetMax = Vector2.zero;

        var txtGO       = new GameObject("Cinematic Text");
        txtGO.transform.SetParent(go.transform, false);
        cinematicText   = txtGO.AddComponent<TextMeshProUGUI>();
        cinematicText.fontSize         = 52;
        cinematicText.color            = Color.white;
        cinematicText.alignment        = TextAlignmentOptions.Center;
        cinematicText.textWrappingMode = TextWrappingModes.Normal;
        cinematicText.alpha            = 0f;
        cinematicText.gameObject.SetActive(false);
        var txtRect = cinematicText.rectTransform;
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(40f, 40f);
        txtRect.offsetMax = new Vector2(-40f, -40f);
    }

    void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        collectClip = SynthTone("Collect", 660f, 0.12f, 0.20f);
        successClip = SynthTone("Success", 880f, 0.30f, 0.35f);
        winSwellClip  = BuildWinSwell();
        failDroneClip = BuildFailDrone();

        swellSource           = gameObject.AddComponent<AudioSource>();
        swellSource.spatialBlend = 0f;
        swellSource.volume    = 0f;
    }

    static AudioClip BuildWinSwell()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 8f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];

        float[] freqs  = { 220f, 275f, 330f, 440f, 550f, 660f };
        float[] amps   = { 0.30f, 0.18f, 0.22f, 0.20f, 0.12f, 0.10f };

        for (int i = 0; i < count; i++)
        {
            float t     = i / (float)rate;
            float ramp  = Mathf.SmoothStep(0f, 1f, t / 2.5f);
            float trail = 1f - Mathf.SmoothStep(0f, 1f, (t - (duration - 1.5f)) / 1.5f);
            float env   = ramp * trail;

            float sample = 0f;
            for (int h = 0; h < freqs.Length; h++)
                sample += Mathf.Sin(2f * Mathf.PI * freqs[h] * t) * amps[h];

            samples[i] = sample * env;
        }

        var clip = AudioClip.Create("WinSwell", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip BuildFailDrone()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 8f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t       = i / (float)rate;
            float ramp    = Mathf.SmoothStep(0f, 1f, t / 3f);
            float tremolo = 1f - 0.25f * Mathf.Sin(2f * Mathf.PI * 0.7f * t);

            samples[i] = (
                Mathf.Sin(2f * Mathf.PI * 55f  * t) * 0.35f +
                Mathf.Sin(2f * Mathf.PI * 82f  * t) * 0.25f +
                Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.15f
            ) * ramp * tremolo;
        }

        var clip = AudioClip.Create("FailDrone", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip SynthTone(string clipName, float freq, float duration, float vol)
    {
        int rate  = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        int count = Mathf.CeilToInt(rate * duration);
        var samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t    = i / (float)rate;
            float fade = 1f - i / (float)count;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * fade;
        }
        var clip = AudioClip.Create(clipName, count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    void ResetAll()
    {
        StopAllCoroutines();

        state               = ObjectiveState.ReadMap;
        TurningPreference.Load();
        winSequenceStarted  = false;
        failSequenceStarted = false;
        sailRenderers.Clear();

        DiscoverSceneObjects();

        if (audioSource == null) BuildAudio();
        if (swellSource != null) { swellSource.Stop(); swellSource.volume = 0f; }

        if (showObjectiveHud && objectiveText == null) BuildHud();
        if (cinematicText == null) BuildCinematicCanvas();

        if (objectiveCanvas != null)   objectiveCanvas.gameObject.SetActive(showObjectiveHud);
        if (cinematicCanvas != null)   cinematicCanvas.gameObject.SetActive(true);
        if (cinematicText != null)     cinematicText.gameObject.SetActive(false);
        if (cinematicVignette != null) cinematicVignette.color = new Color(0f, 0.08f, 0f, 0f);

        UpdateHud();
    }

    void RefreshCamera()
    {
        var cam = Camera.main;
        if (cam != null && cam.gameObject.activeInHierarchy) { cameraRig = cam.transform; return; }
        foreach (var c in Resources.FindObjectsOfTypeAll<Camera>())
            if (c != null && c.enabled && c.gameObject.activeInHierarchy) { cameraRig = c.transform; return; }
    }

    void HandleRestartInput()
    {
        if (state is not (ObjectiveState.Won or ObjectiveState.Failed)) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetAll();
            return;
        }

        if (CheckXrRestartButton()) ResetAll();
    }

    bool _xrRestartHeld;
    bool CheckXrRestartButton()
    {
        try
        {
            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand,  devices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, devices);

            bool held = false;
            foreach (var d in devices)
            {
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool a) && a) { held = true; break; }
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool b) && b) { held = true; break; }
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool m) && m) { held = true; break; }
            }

            bool pressed = held && !_xrRestartHeld;
            _xrRestartHeld = held;
            return pressed;
        }
        catch { return false; }
    }
}
