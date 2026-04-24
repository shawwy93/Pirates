using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SubtitleManager : MonoBehaviour
{
    static SubtitleManager instance;

    Canvas          canvas;
    TextMeshProUGUI label;
    CanvasGroup     group;
    Transform       cameraRig;

    Coroutine activeShow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForActiveScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForActiveScene();

    static void EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        if (instance != null) return;
        var existing = FindAnyObjectByType<SubtitleManager>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[SubtitleManager]");
        instance = go.AddComponent<SubtitleManager>();
        DontDestroyOnLoad(go);
    }

    public static void Show(string text, float duration = 3f)
    {
        if (instance == null) return;
        if (instance.activeShow != null) instance.StopCoroutine(instance.activeShow);
        instance.activeShow = instance.StartCoroutine(instance.ShowRoutine(text, duration));
    }

    public static void Hide()
    {
        if (instance == null || instance.group == null) return;
        if (instance.activeShow != null) instance.StopCoroutine(instance.activeShow);
        instance.group.alpha = 0f;
    }

    void Start() { BuildCanvas(); }

    void Update()
    {
        if (canvas == null) return;
        var cam = Camera.main;
        if (cam == null || !cam.gameObject.activeInHierarchy)
        {
            foreach (var any in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (any == null) continue;
                if (!any.enabled) continue;
                if (!any.gameObject.activeInHierarchy) continue;
                if (any.gameObject.hideFlags != HideFlags.None) continue;
                cam = any;
                break;
            }
        }
        if (cam == null) return;
        if (cameraRig != cam.transform) cameraRig = cam.transform;

        var t = canvas.transform;
        if (t.parent != cameraRig) t.SetParent(cameraRig, false);
        t.localPosition = new Vector3(0f, -0.28f, 1.0f);
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.001f;
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        if (label == null) BuildCanvas();
        label.text = text;

        float fade = 0.25f;
        float e = 0f;
        while (e < fade) { e += Time.deltaTime; group.alpha = Mathf.Clamp01(e / fade); yield return null; }
        group.alpha = 1f;

        yield return new WaitForSeconds(duration);

        e = 0f;
        while (e < fade) { e += Time.deltaTime; group.alpha = 1f - Mathf.Clamp01(e / fade); yield return null; }
        group.alpha = 0f;
        activeShow  = null;
    }

    void BuildCanvas()
    {
        var go = new GameObject("PP Subtitles");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 250;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 120f);

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(go.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        var bgRT = bg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        label = txtGO.AddComponent<TextMeshProUGUI>();
        label.fontSize         = 38;
        label.color            = new Color(1f, 0.96f, 0.82f);
        label.alignment        = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        var tRT = label.rectTransform;
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(24f, 12f);
        tRT.offsetMax = new Vector2(-24f, -12f);

        group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
