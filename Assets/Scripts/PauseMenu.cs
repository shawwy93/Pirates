using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PauseMenu : MonoBehaviour
{
    static PauseMenu instance;

    Canvas canvas;
    CanvasGroup group;
    Transform cameraRig;
    TextMeshProUGUI snapAngleLabel;
    TextMeshProUGUI vignetteLabel;
    TextMeshProUGUI volumeLabel;

    bool isOpen;
    bool menuHeldLastFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (_, __) => EnsureForActiveScene();
        EnsureForActiveScene();
    }

    static void EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        if (instance == null)
        {
            var go = new GameObject("[PauseMenu]");
            instance = go.AddComponent<PauseMenu>();
            DontDestroyOnLoad(go);
        }
    }

    void Start() { BuildUi(); SetOpen(false); }

    void Update()
    {
        if (PollToggle()) SetOpen(!isOpen);

        if (!isOpen || canvas == null) return;

        var cam = Camera.main;
        if (cam == null) return;
        if (cameraRig != cam.transform) cameraRig = cam.transform;

        var t = canvas.transform;
        if (t.parent != cameraRig) t.SetParent(cameraRig, false);
        t.localPosition = new Vector3(0f, -0.05f, 0.7f);
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.0008f;
    }

    bool PollToggle()
    {
        bool held = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        try
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand,  devices);
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, devices);
            foreach (var d in devices)
            {
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool m) && m) { held = true; break; }
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool b) && b) { held = true; break; }
                if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool s) && s) { held = true; break; }
            }
        }
        catch { }

        bool pressed = held && !menuHeldLastFrame;
        menuHeldLastFrame = held;
        return pressed;
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (group != null) group.alpha = open ? 1f : 0f;
        if (canvas != null) canvas.gameObject.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        AudioListener.pause = open;
        RefreshLabels();
    }

    static void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<XRUIInputModule>() == null)
            {
                foreach (var mod in existing.GetComponents<BaseInputModule>())
                    if (mod != null) Destroy(mod);
                existing.gameObject.AddComponent<XRUIInputModule>();
            }
            return;
        }

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<XRUIInputModule>();
    }

    static readonly Color Ink        = new Color(0.14f, 0.07f, 0.03f, 1.00f);
    static readonly Color Parchment  = new Color(0.93f, 0.82f, 0.60f, 0.96f);
    static readonly Color ParchMid   = new Color(0.80f, 0.66f, 0.42f, 0.98f);
    static readonly Color Leather    = new Color(0.31f, 0.19f, 0.09f, 0.98f);

    TMP_FontAsset pirateFont;

    void BuildUi()
    {
        LoadPirateFont();

        var go = new GameObject("PP Pause Menu");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;
        go.AddComponent<TrackedDeviceGraphicRaycaster>();
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(560f, 700f);

        var frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(go.transform, false);
        var frame = frameGO.AddComponent<Image>();
        frame.color = Leather;
        Stretch(frame.rectTransform);

        var pageGO = new GameObject("Parchment");
        pageGO.transform.SetParent(go.transform, false);
        var page = pageGO.AddComponent<Image>();
        page.color = Parchment;
        var pageRT = page.rectTransform;
        pageRT.anchorMin = Vector2.zero; pageRT.anchorMax = Vector2.one;
        pageRT.offsetMin = new Vector2(14f, 14f);
        pageRT.offsetMax = new Vector2(-14f, -14f);

        MakeLabel(go.transform, "PAUSED", 68, new Vector2(0f, 280f), 520f, 90f, FontStyles.Bold, Ink);

        float y = 170f;
        float rowH = 80f;

        MakePlank(go.transform, "Resume",        new Vector2(0f, y), 36, () => SetOpen(false));
        y -= rowH;
        MakePlank(go.transform, "Restart Scene", new Vector2(0f, y), 30, RestartScene);
        y -= rowH + 10f;

        snapAngleLabel = MakeRowLabel(go.transform, new Vector2(-40f, y));
        MakeSmallPlank(go.transform, "-", new Vector2(-210f, y), () => CycleSnap(-1));
        MakeSmallPlank(go.transform, "+", new Vector2( 210f, y), () => CycleSnap(+1));
        y -= rowH;

        vignetteLabel = MakeRowLabel(go.transform, new Vector2(-40f, y));
        MakeSmallPlank(go.transform, "Toggle", new Vector2(210f, y), () =>
        {
            TurningPreference.SetVignetteEnabled(!TurningPreference.VignetteEnabled);
            RefreshLabels();
        }, 90f);
        y -= rowH;

        volumeLabel = MakeRowLabel(go.transform, new Vector2(-40f, y));
        MakeSmallPlank(go.transform, "-", new Vector2(-210f, y), () => NudgeVolume(-0.1f));
        MakeSmallPlank(go.transform, "+", new Vector2( 210f, y), () => NudgeVolume(+0.1f));
        y -= rowH + 20f;

        MakePlank(go.transform, "Quit to Main Menu", new Vector2(0f, y), 30, QuitToMenu);

        group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    void LoadPirateFont()
    {
        if (pirateFont != null) return;
        var ttf = Resources.Load<Font>("Fonts/PirataOne-Regular");
        if (ttf != null)
        {
            try { pirateFont = TMP_FontAsset.CreateFontAsset(ttf); }
            catch { }
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void RefreshLabels()
    {
        if (snapAngleLabel != null) snapAngleLabel.text = $"Snap Angle:  {TurningPreference.SnapAngle}°";
        if (vignetteLabel != null)  vignetteLabel.text  = $"Vignette:  {(TurningPreference.VignetteEnabled ? "ON" : "OFF")}";
        if (volumeLabel != null)    volumeLabel.text    = $"Volume:  {Mathf.RoundToInt(TurningPreference.MasterVolume * 100)}%";
    }

    void CycleSnap(int dir)
    {
        int[] options = { 30, 45, 60 };
        int idx = System.Array.IndexOf(options, TurningPreference.SnapAngle);
        if (idx < 0) idx = 1;
        idx = (idx + dir + options.Length) % options.Length;
        TurningPreference.SetSnapAngle(options[idx]);
        RefreshLabels();
    }

    void NudgeVolume(float delta)
    {
        TurningPreference.SetMasterVolume(TurningPreference.MasterVolume + delta);
        RefreshLabels();
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    void QuitToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);
    }

    TextMeshProUGUI MakeLabel(Transform parent, string text, int size, Vector2 pos, float w, float h, FontStyles style, Color color)
    {
        var go = new GameObject(text + " label");
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<TextMeshProUGUI>();
        l.text = text;
        l.fontSize = size;
        l.fontStyle = style;
        l.alignment = TextAlignmentOptions.Center;
        l.color = color;
        l.textWrappingMode = TextWrappingModes.NoWrap;
        l.characterSpacing = 4f;
        if (pirateFont != null) l.font = pirateFont;
        var rt = l.rectTransform;
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;
        return l;
    }

    TextMeshProUGUI MakeRowLabel(Transform parent, Vector2 pos)
    {
        return MakeLabel(parent, "", 30, pos, 420f, 60f, FontStyles.Normal, Ink);
    }

    void MakePlank(Transform parent, string label, Vector2 pos, int fontSize, System.Action onClick)
    {
        var go = new GameObject("Plank_" + label);
        go.transform.SetParent(parent, false);

        var border = go.AddComponent<Image>();
        border.color = Ink;
        var rt = border.rectTransform;
        rt.sizeDelta = new Vector2(440f, 62f);
        rt.anchoredPosition = pos;

        var innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(go.transform, false);
        var inner = innerGO.AddComponent<Image>();
        inner.color = ParchMid;
        var innerRT = inner.rectTransform;
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(4f, 4f);
        innerRT.offsetMax = new Vector2(-4f, -4f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = inner;
        var colors = btn.colors;
        colors.normalColor      = ParchMid;
        colors.highlightedColor = Parchment;
        colors.pressedColor     = new Color(0.62f, 0.50f, 0.30f, 1f);
        colors.selectedColor    = ParchMid;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txt = MakeLabel(go.transform, label, fontSize, Vector2.zero, 440f, 62f, FontStyles.Bold, Ink);
        txt.rectTransform.anchoredPosition = Vector2.zero;
    }

    void MakeSmallPlank(Transform parent, string label, Vector2 pos, System.Action onClick, float width = 70f)
    {
        var go = new GameObject("SPlank_" + label);
        go.transform.SetParent(parent, false);

        var border = go.AddComponent<Image>();
        border.color = Ink;
        var rt = border.rectTransform;
        rt.sizeDelta = new Vector2(width, 56f);
        rt.anchoredPosition = pos;

        var innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(go.transform, false);
        var inner = innerGO.AddComponent<Image>();
        inner.color = ParchMid;
        var innerRT = inner.rectTransform;
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(3f, 3f);
        innerRT.offsetMax = new Vector2(-3f, -3f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = inner;
        var colors = btn.colors;
        colors.normalColor      = ParchMid;
        colors.highlightedColor = Parchment;
        colors.pressedColor     = new Color(0.62f, 0.50f, 0.30f, 1f);
        colors.selectedColor    = ParchMid;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txt = MakeLabel(go.transform, label, 32, Vector2.zero, width, 56f, FontStyles.Bold, Ink);
        txt.rectTransform.anchoredPosition = Vector2.zero;
    }
}
