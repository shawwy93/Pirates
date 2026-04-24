using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class DesertersNoteDisplay : MonoBehaviour
{
    [Header("Entry")]
    [TextArea(6, 16)]
    [SerializeField] string entry =
        "<i>Scrawled on the back of a rations chit</i>\n\n" +
        "Half the deck wants to strike the colors. Cook says the Crown'll " +
        "hang us at dawn regardless, so what's the good of it. Riggs " +
        "won't hear a word against the sails. Says the wind owes no man " +
        "a wage and the coal-ships will run out of world before they run " +
        "out of us.\n\n" +
        "Old fool. Brave old fool.\n\n" +
        "If I live past the signal, I'm staying on.\n\n" +
        "<align=right><i>Teague, powder monkey</i></align>";

    [Header("Canvas Placement")]
    [Tooltip("Optional anchor child. If set, the canvas parents to this and ignores Offset/Euler below.")]
    [SerializeField] Transform textAnchor;
    [SerializeField] Vector3 canvasLocalOffset = new Vector3(0f, 0.01f, 0f);
    [SerializeField] Vector3 canvasLocalEuler  = new Vector3(-90f, 0f, 0f);
    [SerializeField] Vector2 canvasSize        = new Vector2(0.28f, 0.22f);

    [Header("Appearance")]
    [SerializeField] Color inkColor     = new Color(0.14f, 0.06f, 0.02f);
    [SerializeField] float fontSize     = 0.010f;
    [SerializeField] float fadeDuration = 0.35f;

    XRGrabInteractable grab;
    CanvasGroup        group;
    TextMeshProUGUI    label;
    float              targetAlpha;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        BuildCanvas();
        ApplyEntry();
        if (group != null) group.alpha = 0f;
        targetAlpha = 0f;
    }

    void OnEnable()
    {
        if (grab == null) return;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void Update()
    {
        if (group == null) return;
        if (!Mathf.Approximately(group.alpha, targetAlpha))
        {
            float step = (fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f);
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
        }
    }


    public void SetEntry(string text) { entry = text; ApplyEntry(); }

    void OnGrab(SelectEnterEventArgs _)    => targetAlpha = 1f;
    void OnRelease(SelectExitEventArgs _)  => targetAlpha = 0f;

    void BuildCanvas()
    {
        var existing = transform.Find("DesertersNoteCanvas");
        if (existing != null)
        {
            group = existing.GetComponent<CanvasGroup>();
            label = existing.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        var canvasGO = new GameObject("DesertersNoteCanvas");
        if (textAnchor != null)
        {
            canvasGO.transform.SetParent(textAnchor, false);
            canvasGO.transform.localPosition    = Vector3.zero;
            canvasGO.transform.localEulerAngles = Vector3.zero;
        }
        else
        {
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition    = canvasLocalOffset;
            canvasGO.transform.localEulerAngles = canvasLocalEuler;
        }

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit   = 100f;
        scaler.referencePixelsPerUnit = 100f;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(canvasSize.x * 1000f, canvasSize.y * 1000f);
        rt.localScale = Vector3.one * 0.001f;

        group = canvasGO.AddComponent<CanvasGroup>();
        group.interactable   = false;
        group.blocksRaycasts = false;

        var textGO = new GameObject("DesertersNoteText");
        textGO.transform.SetParent(canvasGO.transform, false);
        label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment        = TextAlignmentOptions.TopLeft;
        label.color            = inkColor;
        label.fontSize         = fontSize * 1000f;
        label.fontStyle        = FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.lineSpacing      = 6f;
        label.paragraphSpacing = 8f;

        var ttf = Resources.Load<Font>("Fonts/PirataOne-Regular");
        if (ttf != null)
        {
            try { label.font = TMP_FontAsset.CreateFontAsset(ttf); }
            catch { }
        }

        var textRT = label.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(22f, 18f);
        textRT.offsetMax = new Vector2(-22f, -18f);
    }

    void ApplyEntry()
    {
        if (label != null) label.text = entry;
    }
}
