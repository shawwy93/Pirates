using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene to load")]
    [SerializeField] string gameSceneName = "mainPirate";

    [Header("Typography")]
    [Tooltip("Display font for the menu — a pirate-era serif or blackletter reads best. " +
             "Leave blank to fall back to TMP's default. Dress Main Menu will auto-find one if you drop a TTF in Assets/Fonts.")]
    [SerializeField] TMP_FontAsset pirateFont;

    [Header("Map Backdrop")]
    [Tooltip("The pirate-map prefab used as the menu backdrop. Wire this to SM_Item_Map_03.")]
    [SerializeField] GameObject mapBackdropPrefab;

    [Tooltip("World-space scale applied to the instantiated map backdrop. Tune until the map fills the menu area.")]
    [SerializeField] Vector3 mapBackdropScale = new Vector3(6f, 6f, 6f);

    [Tooltip("Euler rotation applied to the instantiated map (relative to the canvas facing). 90,0,0 lays a flat map upright facing the player.")]
    [SerializeField] Vector3 mapBackdropLocalEuler = new Vector3(90f, 0f, 0f);

    [Tooltip("Depth offset behind the canvas (negative Z pushes it back).")]
    [SerializeField] float mapBackdropDepth = -0.15f;

    Transform cameraRig;
    bool      transitioning;

    Button smoothBtn;
    Button snapBtn;

    TextMeshProUGUI snapAngleValue;
    TextMeshProUGUI vignetteValue;
    TextMeshProUGUI volumeValue;

    GameObject mainPanelRoot;
    GameObject comfortPanelRoot;

    static readonly Color Ink        = new Color(0.14f, 0.07f, 0.03f, 1.00f);
    static readonly Color InkFaded   = new Color(0.36f, 0.22f, 0.09f, 1.00f);
    static readonly Color Parchment  = new Color(0.93f, 0.82f, 0.60f, 0.92f);
    static readonly Color ParchMid   = new Color(0.80f, 0.66f, 0.42f, 0.95f);
    static readonly Color ParchDark  = new Color(0.55f, 0.38f, 0.18f, 0.96f);
    static readonly Color Leather    = new Color(0.31f, 0.19f, 0.09f, 0.97f);
    static readonly Color LeatherDk  = new Color(0.16f, 0.09f, 0.04f, 0.98f);
    static readonly Color WaxRed     = new Color(0.58f, 0.17f, 0.11f, 0.98f);
    static readonly Color WaxDeep    = new Color(0.32f, 0.07f, 0.05f, 0.98f);
    static readonly Color Brass      = new Color(0.78f, 0.58f, 0.22f, 1.00f);
    static readonly Color BrassDeep  = new Color(0.40f, 0.28f, 0.08f, 1.00f);
    static readonly Color Cream      = new Color(0.96f, 0.90f, 0.72f, 1.00f);
    static readonly Color CreamDim   = new Color(0.78f, 0.68f, 0.48f, 1.00f);
    static readonly Color Iron       = new Color(0.11f, 0.08f, 0.05f, 1.00f);
    static readonly Color IronHi     = new Color(0.50f, 0.38f, 0.22f, 0.80f);
    static readonly Color Sheen      = new Color(1.00f, 0.92f, 0.70f, 0.10f);

    Transform canvasAnchor;
    Quaternion baseCanvasRotation;

    void Start()
    {
        TurningPreference.Load();
        LockPlayerLocomotion();
        EnsureEventSystem();
        AutoLoadPirateFont();
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {

        yield return null;
        RefreshCamera();

        var (cardGO, cardGroup) = BuildTitleCard();
        cardGroup.alpha = 1f;
        Debug.Log("[TitleCard] visible at scene start");

        BuildMenu();
        CanvasGroup menuGroup = null;
        if (canvasAnchor != null)
        {

            menuGroup = canvasAnchor.GetComponent<CanvasGroup>();
            if (menuGroup == null)
                menuGroup = canvasAnchor.gameObject.AddComponent<CanvasGroup>();

            if (menuGroup != null)
            {
                menuGroup.alpha          = 0f;
                menuGroup.interactable   = false;
                menuGroup.blocksRaycasts = false;
            }
        }

        yield return new WaitForSeconds(3.0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.8f;
            cardGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        Destroy(cardGO);
        Debug.Log("[TitleCard] finished — menu revealing");

        if (menuGroup != null)
        {
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.55f;
                menuGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
            menuGroup.alpha          = 1f;
            menuGroup.interactable   = true;
            menuGroup.blocksRaycasts = true;
        }
    }

    void AutoLoadPirateFont()
    {
        if (pirateFont != null) return;

        var ttf = Resources.Load<Font>("Fonts/PirataOne-Regular");
        if (ttf == null)
        {
            Debug.LogWarning(
                "[MainMenu] Resources/Fonts/PirataOne-Regular.ttf not found — " +
                "menu will use the default TMP font.");
            return;
        }

        try
        {
            pirateFont = TMP_FontAsset.CreateFontAsset(ttf);
            pirateFont.name = "PirataOne SDF (runtime)";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MainMenu] Failed to create pirate TMP font: " + e.Message);
            pirateFont = null;
        }
    }

    (GameObject, CanvasGroup) BuildTitleCard()
    {
        var cardGO = new GameObject("TitleCard");

        if (cameraRig != null)
        {
            cardGO.transform.SetParent(cameraRig, false);
            cardGO.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            cardGO.transform.localRotation = Quaternion.identity;
            cardGO.transform.localScale    = Vector3.one * 0.0022f;
        }
        else
        {
            cardGO.transform.position   = new Vector3(0f, 1.6f, 2.2f);
            cardGO.transform.localScale = Vector3.one * 0.0022f;
        }

        var cardCanvas = cardGO.AddComponent<Canvas>();
        cardCanvas.renderMode   = RenderMode.WorldSpace;
        cardCanvas.sortingOrder = 500;
        var cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(1600f, 900f);

        var cardGroup = cardGO.AddComponent<CanvasGroup>();
        cardGroup.alpha = 1f;
        cardGroup.interactable   = false;
        cardGroup.blocksRaycasts = false;

        var blackGO  = new GameObject("Black");
        blackGO.transform.SetParent(cardGO.transform, false);
        var blackImg = blackGO.AddComponent<Image>();
        blackImg.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        Stretch(blackImg.rectTransform);

        var ruleTop = MakeImage(cardGO.transform, "RuleTop",
            new Color(CreamDim.r, CreamDim.g, CreamDim.b, 0.55f));
        SetAnchors(ruleTop.rectTransform,
            new Vector2(0.22f, 0.615f), new Vector2(0.78f, 0.622f));

        var ruleBot = MakeImage(cardGO.transform, "RuleBottom",
            new Color(CreamDim.r, CreamDim.g, CreamDim.b, 0.55f));
        SetAnchors(ruleBot.rectTransform,
            new Vector2(0.22f, 0.378f), new Vector2(0.78f, 0.385f));

        MakeEngravedText(cardGO.transform, "PIRATE'S  PLIGHT", 140,
            Cream, new Color(Brass.r, Brass.g, Brass.b, 0.55f),
            FontStyles.Bold | FontStyles.UpperCase,
            new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.60f),
            characterSpacing: 20f);

        var subtitle = MakeText(cardGO.transform,
            "A Renewable Energy Adventure", 34, CreamDim,
            FontStyles.Italic,
            TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.39f));
        subtitle.characterSpacing = 6f;

        var diamond = MakeImage(cardGO.transform, "Flourish", Brass);
        var drt = diamond.rectTransform;
        drt.anchorMin = new Vector2(0.5f, 0.495f);
        drt.anchorMax = new Vector2(0.5f, 0.505f);
        drt.sizeDelta = new Vector2(14f, 14f);
        drt.anchoredPosition = Vector2.zero;
        diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        return (cardGO, cardGroup);
    }

    void BuildMenu()
    {

        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var rt       = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(760f, 560f);

        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        canvasGO.AddComponent<GraphicRaycaster>();

        PlaceCanvas(canvasGO.transform);

        canvasAnchor       = canvasGO.transform;
        baseCanvasRotation = canvasAnchor.rotation;

        SpawnMapBackdrop(canvasGO.transform);

        Transform root = canvasGO.transform;

        mainPanelRoot    = BuildContainer(root, "MainPanel");
        comfortPanelRoot = BuildContainer(root, "ComfortSubmenu");

        BuildMainScreen(mainPanelRoot.transform);
        BuildComfortScreen(comfortPanelRoot.transform);

        ShowComfortScreen(false);
    }

    GameObject BuildContainer(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    void ShowComfortScreen(bool show)
    {
        if (mainPanelRoot    != null) mainPanelRoot.SetActive(!show);
        if (comfortPanelRoot != null) comfortPanelRoot.SetActive(show);
        if (show) RefreshComfortVisuals();
    }

    void BuildMainScreen(Transform root)
    {
        BuildTitlePlaque(root, "PIRATE'S PLIGHT",
            new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.965f));

        var startBtn = BuildOrnatePlank(root, "SET SAIL", fontSize: 38,
            new Vector2(0.09f, 0.55f), new Vector2(0.91f, 0.72f),
            body: Parchment, border: Ink, textColor: Ink, showWaxSeal: true);
        startBtn.onClick.AddListener(() =>
        {
            if (!transitioning) StartCoroutine(LoadGame());
        });

        var comfortBtn = BuildOrnatePlank(root, "HELMSMAN'S COMFORT", fontSize: 26,
            new Vector2(0.09f, 0.32f), new Vector2(0.91f, 0.50f),
            body: ParchMid, border: Ink, textColor: Ink, showWaxSeal: false);
        comfortBtn.onClick.AddListener(() => ShowComfortScreen(true));

        var quitBtn = BuildOrnatePlank(root, "ABANDON SHIP", fontSize: 22,
            new Vector2(0.22f, 0.07f), new Vector2(0.78f, 0.21f),
            body: ParchMid, border: InkFaded, textColor: Ink, showWaxSeal: false);
        quitBtn.onClick.AddListener(() => Application.Quit());

#if UNITY_EDITOR
        foreach (var t in quitBtn.GetComponentsInChildren<TextMeshProUGUI>())
            t.color = new Color(t.color.r, t.color.g, t.color.b, t.color.a * 0.5f);
#endif
    }

    void BuildComfortScreen(Transform root)
    {

        BuildTitlePlaque(root, "HELMSMAN'S COMFORT",
            new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.965f));

        BuildComfortPanel(root,
            new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f));

        var backBtn = BuildOrnatePlank(root, "BACK", fontSize: 24,
            new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.18f),
            body: ParchMid, border: InkFaded, textColor: Ink, showWaxSeal: false);
        backBtn.onClick.AddListener(() => ShowComfortScreen(false));
    }

    void BuildTitlePlaque(Transform parent, string title, Vector2 aMin, Vector2 aMax)
    {

        var faintStrip = MakeImage(parent, "TitleFaintPaper",
            new Color(Parchment.r, Parchment.g, Parchment.b, 0.35f));
        SetAnchors(faintStrip.rectTransform, aMin, aMax);

        AddDropShadow(faintStrip.gameObject, new Vector2(3, -5), 0.35f);

        MakeEngravedText(faintStrip.transform, title, 62, Ink, InkFaded,
            FontStyles.Bold | FontStyles.UpperCase,
            new Vector2(0.02f, 0.24f), new Vector2(0.98f, 0.86f),
            characterSpacing: 12f);

        var rule = MakeImage(faintStrip.transform, "InkRule", InkFaded);
        SetAnchors(rule.rectTransform,
            new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.18f));

        var diamond = MakeImage(faintStrip.transform, "FlourishDiamond", Ink);
        var drt = diamond.rectTransform;
        drt.anchorMin = new Vector2(0.5f, 0.155f);
        drt.anchorMax = new Vector2(0.5f, 0.195f);
        drt.sizeDelta = new Vector2(12f, 12f);
        drt.anchoredPosition = Vector2.zero;
        diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        AddInkDot(faintStrip.transform, new Vector2(0.14f, 0.17f));
        AddInkDot(faintStrip.transform, new Vector2(0.86f, 0.17f));
    }

    static void AddInkDot(Transform parent, Vector2 anchor)
    {
        var dot = new GameObject("InkDot");
        dot.transform.SetParent(parent, false);
        var img = dot.AddComponent<Image>();
        img.color = Ink;
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(5f, 5f);
        rt.anchoredPosition = Vector2.zero;
    }

    void BuildComfortPanel(Transform parent, Vector2 aMin, Vector2 aMax)
    {

        var panel = MakeImage(parent, "ComfortPanel", ParchDark);
        SetAnchors(panel.rectTransform, aMin, aMax);
        AddDropShadow(panel.gameObject, new Vector2(3, -4), 0.4f);

        var recess = MakeImage(panel.transform, "Recess", Parchment);
        StretchWithInset(recess.rectTransform, 0.02f);
        AddOutline(recess.gameObject, Ink, new Vector2(1, -1));

        AddRivet(panel.transform, new Vector2(0.03f, 0.94f), 10f);
        AddRivet(panel.transform, new Vector2(0.97f, 0.94f), 10f);
        AddRivet(panel.transform, new Vector2(0.03f, 0.06f), 10f);
        AddRivet(panel.transform, new Vector2(0.97f, 0.06f), 10f);

        MakeEngravedText(recess.transform, "HELMSMAN'S COMFORT", 16, Ink, InkFaded,
            FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase,
            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.98f),
            characterSpacing: 8f);

        var rule = MakeImage(recess.transform, "HeaderRule", InkFaded);
        SetAnchors(rule.rectTransform,
            new Vector2(0.18f, 0.82f), new Vector2(0.82f, 0.832f));

        BuildRowLabel(recess.transform, "TURNING",
            new Vector2(0.04f, 0.68f), new Vector2(0.30f, 0.80f));

        smoothBtn = BuildTabButton(recess.transform, "SMOOTH",
            new Vector2(0.32f, 0.66f), new Vector2(0.62f, 0.80f));
        snapBtn = BuildTabButton(recess.transform, "SNAP",
            new Vector2(0.64f, 0.66f), new Vector2(0.94f, 0.80f));

        smoothBtn.onClick.AddListener(() => SetTurning(TurningPreference.Mode.Smooth));
        snapBtn.onClick.AddListener(()   => SetTurning(TurningPreference.Mode.Snap));

        BuildRowLabel(recess.transform, "SNAP ANGLE",
            new Vector2(0.04f, 0.50f), new Vector2(0.30f, 0.62f));

        var minusSnap = BuildTabButton(recess.transform, "–",
            new Vector2(0.32f, 0.48f), new Vector2(0.44f, 0.62f));
        snapAngleValue = MakeText(recess.transform, "45°", 26, Ink,
            FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.46f, 0.48f), new Vector2(0.80f, 0.62f));
        var plusSnap = BuildTabButton(recess.transform, "+",
            new Vector2(0.82f, 0.48f), new Vector2(0.94f, 0.62f));

        minusSnap.onClick.AddListener(() => CycleSnapAngle(-1));
        plusSnap.onClick.AddListener(()  => CycleSnapAngle(+1));

        BuildRowLabel(recess.transform, "VIGNETTE",
            new Vector2(0.04f, 0.32f), new Vector2(0.30f, 0.44f));

        var vignetteBtn = BuildTabButton(recess.transform, "TOGGLE",
            new Vector2(0.32f, 0.30f), new Vector2(0.62f, 0.44f));
        vignetteValue = MakeText(recess.transform, "OFF", 24, Ink,
            FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.64f, 0.30f), new Vector2(0.94f, 0.44f));

        vignetteBtn.onClick.AddListener(() =>
        {
            TurningPreference.SetVignetteEnabled(!TurningPreference.VignetteEnabled);
            RefreshComfortVisuals();
        });

        BuildRowLabel(recess.transform, "VOLUME",
            new Vector2(0.04f, 0.14f), new Vector2(0.30f, 0.26f));

        var minusVol = BuildTabButton(recess.transform, "–",
            new Vector2(0.32f, 0.12f), new Vector2(0.44f, 0.26f));
        volumeValue = MakeText(recess.transform, "100%", 24, Ink,
            FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.46f, 0.12f), new Vector2(0.80f, 0.26f));
        var plusVol = BuildTabButton(recess.transform, "+",
            new Vector2(0.82f, 0.12f), new Vector2(0.94f, 0.26f));

        minusVol.onClick.AddListener(() => NudgeVolume(-0.1f));
        plusVol.onClick.AddListener(()  => NudgeVolume(+0.1f));

        RefreshComfortVisuals();
    }

    void BuildRowLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax)
    {
        var t = MakeText(parent, text, 18, Ink,
            FontStyles.Bold | FontStyles.UpperCase,
            TextAlignmentOptions.MidlineLeft, aMin, aMax);
        t.characterSpacing = 4f;
    }

    void CycleSnapAngle(int dir)
    {
        int[] options = { 30, 45, 60 };
        int idx = System.Array.IndexOf(options, TurningPreference.SnapAngle);
        if (idx < 0) idx = 1;
        idx = (idx + dir + options.Length) % options.Length;
        TurningPreference.SetSnapAngle(options[idx]);
        RefreshComfortVisuals();
    }

    void NudgeVolume(float delta)
    {
        TurningPreference.SetMasterVolume(TurningPreference.MasterVolume + delta);
        RefreshComfortVisuals();
    }

    static void AddWaxSeal(Transform parent, Vector2 anchor)
    {
        var seal = new GameObject("WaxSeal");
        seal.transform.SetParent(parent, false);
        var img = seal.AddComponent<Image>();
        img.color = WaxRed;
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(20f, 20f);
        rt.anchoredPosition = Vector2.zero;
        img.transform.localRotation = Quaternion.Euler(0, 0, 15f);

        var inner = new GameObject("Core");
        inner.transform.SetParent(seal.transform, false);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = WaxDeep;
        var irt = innerImg.rectTransform;
        irt.anchorMin = new Vector2(0.2f, 0.2f);
        irt.anchorMax = new Vector2(0.8f, 0.8f);
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
    }

    Button BuildOrnatePlank(Transform parent, string label, float fontSize,
        Vector2 aMin, Vector2 aMax,
        Color body, Color border, Color textColor, bool showWaxSeal)
    {

        var root    = new GameObject(label);
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = ParchDark;
        SetAnchors(rootImg.rectTransform, aMin, aMax);
        AddDropShadow(root, new Vector2(4, -6), 0.45f);

        var body_img = MakeImage(root.transform, "Body", body);
        StretchWithInset(body_img.rectTransform, 0.025f);

        AddOutline(body_img.gameObject, border, new Vector2(1, -1));

        var sheen = MakeImage(body_img.transform, "Sheen", Sheen);
        SetAnchors(sheen.rectTransform,
            new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.92f));

        AddRivet(body_img.transform, new Vector2(0.04f, 0.85f), 10f);
        AddRivet(body_img.transform, new Vector2(0.96f, 0.85f), 10f);
        AddRivet(body_img.transform, new Vector2(0.04f, 0.15f), 10f);
        AddRivet(body_img.transform, new Vector2(0.96f, 0.15f), 10f);

        if (showWaxSeal)
            AddWaxSeal(body_img.transform, new Vector2(0.92f, 0.22f));

        MakeEngravedText(body_img.transform, label, fontSize, textColor,
            new Color(InkFaded.r, InkFaded.g, InkFaded.b, 0.7f),
            FontStyles.Bold | FontStyles.UpperCase,
            new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f),
            characterSpacing: 8f);

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = body_img;
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.05f, 0.85f);
        colors.pressedColor     = new Color(0.6f, 0.6f, 0.6f);
        colors.selectedColor    = Color.white;
        colors.colorMultiplier  = 1f;
        btn.colors = colors;

        return btn;
    }

    Button BuildTabButton(Transform parent, string label,
        Vector2 aMin, Vector2 aMax)
    {
        var root    = new GameObject(label);
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = LeatherDk;
        SetAnchors(rootImg.rectTransform, aMin, aMax);
        AddDropShadow(root, new Vector2(2, -3), 0.45f);

        var body = MakeImage(root.transform, "Body", Leather);
        StretchWithInset(body.rectTransform, 0.05f);
        AddOutline(body.gameObject, Ink, new Vector2(1, -1));

        var sheen = MakeImage(body.transform, "Sheen", Sheen);
        SetAnchors(sheen.rectTransform,
            new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.92f));

        AddRivet(body.transform, new Vector2(0.08f, 0.80f), 8f);
        AddRivet(body.transform, new Vector2(0.92f, 0.80f), 8f);
        AddRivet(body.transform, new Vector2(0.08f, 0.20f), 8f);
        AddRivet(body.transform, new Vector2(0.92f, 0.20f), 8f);

        MakeEngravedText(body.transform, label, 22, Cream,
            new Color(0f, 0f, 0f, 0.6f),
            FontStyles.Bold | FontStyles.UpperCase,
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.90f),
            characterSpacing: 5f);

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = body;
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.06f, 0.88f);
        colors.pressedColor     = new Color(0.7f,  0.65f, 0.55f);
        btn.colors = colors;

        return btn;
    }

    static void AddRivet(Transform parent, Vector2 anchor, float size = 12f)
    {
        var rivet = new GameObject("Rivet");
        rivet.transform.SetParent(parent, false);
        var img = rivet.AddComponent<Image>();
        img.color = Iron;
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        var hi = new GameObject("Highlight");
        hi.transform.SetParent(rivet.transform, false);
        var himg = hi.AddComponent<Image>();
        himg.color = IronHi;
        var hrt = himg.rectTransform;
        hrt.anchorMin = new Vector2(0.18f, 0.50f);
        hrt.anchorMax = new Vector2(0.55f, 0.85f);
        hrt.offsetMin = Vector2.zero;
        hrt.offsetMax = Vector2.zero;
    }

    static void AddDropShadow(GameObject go, Vector2 distance, float alpha)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectColor    = new Color(0f, 0f, 0f, alpha);
        sh.effectDistance = distance;
    }

    static void StretchWithInset(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(inset, inset);
        rt.anchorMax = new Vector2(1f - inset, 1f - inset);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void MakeEngravedText(Transform parent, string text, float fontSize,
        Color surface, Color engraved, FontStyles style,
        Vector2 aMin, Vector2 aMax, float characterSpacing = 0f)
    {

        var shadow = MakeText(parent, text, fontSize, engraved, style,
            TextAlignmentOptions.Center, aMin, aMax,
            new Vector2(2, -2), new Vector2(2, -2));
        shadow.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        shadow.characterSpacing = characterSpacing;
        shadow.name = "EngraveShadow";

        var face = MakeText(parent, text, fontSize, surface, style,
            TextAlignmentOptions.Center, aMin, aMax);
        face.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        face.characterSpacing = characterSpacing;
        face.name = "EngraveFace";
    }

    void SpawnMapBackdrop(Transform canvasTransform)
    {
        if (mapBackdropPrefab == null)
        {
            Debug.LogWarning("[MainMenu] mapBackdropPrefab not assigned — skipping map backdrop.");
            return;
        }

        var map = Instantiate(mapBackdropPrefab);
        map.name = "MapBackdrop";

        map.transform.SetParent(canvasTransform, false);

        map.transform.localPosition = new Vector3(0f, 0f, -mapBackdropDepth * 1000f);
        map.transform.localRotation = Quaternion.Euler(mapBackdropLocalEuler);

        float inv = 1f / Mathf.Max(0.0001f, canvasTransform.localScale.x);
        map.transform.localScale = new Vector3(
            mapBackdropScale.x * inv,
            mapBackdropScale.y * inv,
            mapBackdropScale.z * inv);

        foreach (var col in map.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var rb in map.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    static Image MakePlate(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var img = MakeImage(parent, name, color);
        SetAnchors(img.rectTransform, anchorMin, anchorMax);
        return img;
    }

    void PlaceCanvas(Transform canvasTransform)
    {
        Vector3 forward = Vector3.forward;
        Vector3 origin  = new Vector3(0f, 1.6f, 0f);

        if (cameraRig != null)
        {
            origin  = cameraRig.position;
            forward = cameraRig.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
        }

        canvasTransform.position = origin + forward * 2.5f;
        canvasTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        canvasTransform.localScale = Vector3.one * 0.0026f;
    }

    void SetTurning(TurningPreference.Mode mode)
    {
        TurningPreference.Set(mode);
        RefreshComfortVisuals();
    }

    void RefreshComfortVisuals()
    {
        bool isSnap = TurningPreference.Current == TurningPreference.Mode.Snap;
        SetTabBodyColor(smoothBtn, isSnap ? Leather : WaxRed);
        SetTabBodyColor(snapBtn,   isSnap ? WaxRed  : Leather);

        if (snapAngleValue != null) snapAngleValue.text = $"{TurningPreference.SnapAngle}°";
        if (vignetteValue  != null) vignetteValue.text  = TurningPreference.VignetteEnabled ? "ON" : "OFF";
        if (volumeValue    != null) volumeValue.text    = $"{Mathf.RoundToInt(TurningPreference.MasterVolume * 100)}%";
    }

    static void SetTabBodyColor(Button btn, Color color)
    {
        if (btn == null) return;
        var body = btn.transform.Find("Body");
        if (body == null) return;
        var img = body.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    void Update()
    {
        if (canvasAnchor == null) return;

        float roll  = Mathf.Sin(Time.time * 0.62f) * 0.9f;
        float pitch = Mathf.Sin(Time.time * 0.41f + 1.3f) * 0.4f;

        canvasAnchor.rotation = baseCanvasRotation * Quaternion.Euler(pitch, 0f, roll);
    }

    IEnumerator LoadGame()
    {
        transitioning = true;
        RefreshCamera();

        Image fadeImage = null;
        if (cameraRig != null)
        {
            var fadeGO = new GameObject("Fade");
            fadeGO.transform.SetParent(cameraRig, false);
            fadeGO.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            fadeGO.transform.localRotation = Quaternion.identity;
            fadeGO.transform.localScale    = Vector3.one * 0.002f;

            var fc = fadeGO.AddComponent<Canvas>();
            fc.renderMode   = RenderMode.WorldSpace;
            fc.sortingOrder = 999;
            fc.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 1400f);

            var imgGO  = new GameObject("Black");
            imgGO.transform.SetParent(fadeGO.transform, false);
            fadeImage  = imgGO.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            Stretch(fadeImage.rectTransform);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.1f;
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    static void LockPlayerLocomotion()
    {
        foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mono == null) continue;
            var tn = mono.GetType().Name;
            if (tn == "ContinuousMoveProvider" ||
                tn == "ContinuousMoveProviderBase" ||
                tn == "DynamicMoveProvider" ||
                tn == "TeleportationProvider" ||
                tn == "SnapTurnProvider" ||
                tn == "ContinuousTurnProvider" ||
                tn == "FPS_Movement" ||
                tn == "GrabMoveProvider" ||
                tn == "TwoHandedGrabMoveProvider")
            {
                mono.enabled = false;
            }
        }
    }

    static void EnsureEventSystem()
    {
        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<XRUIInputModule>() == null)
            {
                foreach (var mod in existing.GetComponents<BaseInputModule>())
                    if (mod != null) Object.Destroy(mod);
                existing.gameObject.AddComponent<XRUIInputModule>();
            }
            return;
        }

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<XRUIInputModule>();
    }

    static Button MakeButton(Transform parent, string label,
        Color textColor, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(label);
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.color = bgColor;
        SetAnchors(img.rectTransform, anchorMin, anchorMax);
        AddOutline(go, new Color(0f, 0f, 0f, 0.4f), new Vector2(1, -1));

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = bgColor;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.18f);
        colors.pressedColor     = Color.Lerp(bgColor, Color.black, 0.22f);
        colors.selectedColor    = colors.normalColor;
        btn.colors = colors;

        var textGO  = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tmp     = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 28;
        tmp.color     = textColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        Stretch(tmp.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));

        return btn;
    }

    TextMeshProUGUI MakeText(Transform parent, string text,
        float fontSize, Color color, FontStyles style,
        TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go  = new GameObject(text.Length > 20 ? text[..20] : text);
        go.transform.SetParent(parent, false);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        if (pirateFont != null) tmp.font = pirateFont;
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        SetAnchors(tmp.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        return tmp;
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go    = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img   = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline          = go.AddComponent<Outline>();
        outline.effectColor  = color;
        outline.effectDistance = distance;
    }

    static void SetAnchors(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void Stretch(RectTransform rt,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    void RefreshCamera()
    {
        var cam = Camera.main;
        if (cam != null && cam.gameObject.activeInHierarchy) { cameraRig = cam.transform; return; }
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c != null && c.enabled && c.gameObject.activeInHierarchy) { cameraRig = c.transform; return; }
    }
}
