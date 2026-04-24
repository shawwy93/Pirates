using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MovementVignette : MonoBehaviour
{
    static MovementVignette instance;

    [Range(0f, 1f)] public float maxOpacity       = 0.85f;
    [Range(0f, 1f)] public float innerTransparent = 0.55f;
    public float fadeInSpeed  = 4f;
    public float fadeOutSpeed = 3f;

    Canvas canvas;
    RawImage ring;
    Transform cameraRig;
    Vector3 lastCamPos;
    Quaternion lastCamRot;
    float currentAlpha;
    Texture2D ringTex;

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
            var go = new GameObject("[MovementVignette]");
            instance = go.AddComponent<MovementVignette>();
            DontDestroyOnLoad(go);
        }
    }

    void Start() { BuildCanvas(); }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null || canvas == null) return;

        if (cameraRig != cam.transform)
        {
            cameraRig  = cam.transform;
            lastCamPos = cameraRig.position;
            lastCamRot = cameraRig.rotation;
        }

        var t = canvas.transform;
        if (t.parent != cameraRig) t.SetParent(cameraRig, false);
        t.localPosition = new Vector3(0f, 0f, 0.3f);
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one * 0.0008f;

        float linSpeed  = (cameraRig.position - lastCamPos).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
        float angSpeed  = Quaternion.Angle(lastCamRot, cameraRig.rotation) / Mathf.Max(Time.deltaTime, 1e-4f);
        lastCamPos = cameraRig.position;
        lastCamRot = cameraRig.rotation;

        float motion = Mathf.Clamp01(linSpeed / 2.5f + angSpeed / 160f);

        float target = TurningPreference.VignetteEnabled ? motion : 0f;
        float speed  = target > currentAlpha ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, speed * Time.deltaTime);

        if (ring != null)
        {
            var c = ring.color; c.a = currentAlpha * maxOpacity; ring.color = c;
            ring.enabled = currentAlpha > 0.001f && TurningPreference.VignetteEnabled;
        }
    }

    void BuildCanvas()
    {
        var go = new GameObject("PP Comfort Vignette");
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 400;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1200f, 900f);

        var ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(go.transform, false);
        ring = ringGO.AddComponent<RawImage>();
        ring.raycastTarget = false;
        ring.texture = BuildRingTexture(512);
        ring.color   = new Color(0f, 0f, 0f, 0f);
        var rRT = ring.rectTransform;
        rRT.anchorMin = Vector2.zero;
        rRT.anchorMax = Vector2.one;
        rRT.offsetMin = rRT.offsetMax = Vector2.zero;
    }

    Texture2D BuildRingTexture(int size)
    {
        ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pix = new Color32[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / maxR;

            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerTransparent, 1f, d));
            pix[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.Clamp(a * 255f, 0, 255));
        }
        ringTex.SetPixels32(pix);
        ringTex.Apply();
        return ringTex;
    }
}
