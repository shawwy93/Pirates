using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class MapRiddleDisplay : MonoBehaviour
{
    [Header("Riddle")]
    [TextArea(3, 8)]
    [SerializeField] string riddle =

        "Where the crow keeps watch,\n" +
        "he sees the smoke first.\n" +
        "The key hangs where the wind blows cleanest,\n" +
        "above the deck, below the stars.";

    [Header("Canvas Placement")]
    [Tooltip("Optional anchor child. If set, the canvas parents to this and ignores Offset/Euler below.")]
    [SerializeField] Transform textAnchor;
    [SerializeField] Vector3 canvasLocalOffset = new Vector3(0f, 0.01f, 0f);
    [SerializeField] Vector3 canvasLocalEuler  = new Vector3(-90f, 0f, 0f);
    [SerializeField] Vector2 canvasSize        = new Vector2(0.35f, 0.22f);

    [Header("Appearance")]
    [SerializeField] Color inkColor = new Color(0.18f, 0.09f, 0.02f);
    [SerializeField] float fontSize = 0.022f;
    [SerializeField] float fadeDuration = 0.35f;

    XRGrabInteractable grab;
    CanvasGroup        group;
    TextMeshProUGUI    label;
    float              targetAlpha;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        BuildCanvas();
        ApplyRiddle();
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


    public void SetRiddle(string text)
    {
        riddle = text;
        ApplyRiddle();
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        targetAlpha = 1f;

        PirateObjectiveController.NotifyMapRead();
    }

    void OnRelease(SelectExitEventArgs _)
        => targetAlpha = 0f;

    void BuildCanvas()
    {

        var existing = transform.Find("RiddleCanvas");
        if (existing != null)
        {
            group = existing.GetComponent<CanvasGroup>();
            label = existing.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        var canvasGO = new GameObject("RiddleCanvas");
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
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        scaler.referencePixelsPerUnit = 100f;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(canvasSize.x * 1000f, canvasSize.y * 1000f);
        rt.localScale = Vector3.one * 0.001f;

        group = canvasGO.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var textGO = new GameObject("RiddleText");
        textGO.transform.SetParent(canvasGO.transform, false);

        label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.color = inkColor;
        label.fontSize = fontSize * 1000f;
        label.fontStyle = FontStyles.Italic;
        label.textWrappingMode = TextWrappingModes.Normal;

        var pirateTtf = Resources.Load<Font>("Fonts/PirataOne-Regular");
        if (pirateTtf != null)
        {
            try { label.font = TMP_FontAsset.CreateFontAsset(pirateTtf); }
            catch {  }
        }

        var textRT = label.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20f, 20f);
        textRT.offsetMax = new Vector2(-20f, -20f);
    }

    void ApplyRiddle()
    {
        if (label != null) label.text = riddle;
    }
}
