using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HorizonSmoke : MonoBehaviour
{
    static HorizonSmoke instance;

    [Header("Quad")]
    public Vector2 size = new Vector2(140f, 60f);
    public Color smokeColor = new Color(0.06f, 0.06f, 0.07f, 1f);

    [Header("Reveal")]
    public float fadeInSeconds = 3.5f;
    public bool revealOnStart = false;

    [Header("Drift")]
    public float driftAmplitude = 2.5f;
    public float driftSpeed = 0.18f;

    GameObject quad;
    Material mat;
    Vector3 basePos;
    float phaseX, phaseY;
    float currentAlpha, targetAlpha;
    bool revealed;
    System.Collections.Generic.HashSet<XRBaseInteractable> hooked =
        new System.Collections.Generic.HashSet<XRBaseInteractable>();

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

        var existing = FindAnyObjectByType<HorizonSmoke>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[HorizonSmoke]");
        go.transform.position = ComputeSmokePosition();
        instance = go.AddComponent<HorizonSmoke>();
    }

    static Vector3 ComputeSmokePosition()
    {
        Transform ship = null;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null) continue;
            if (t.gameObject.scene != SceneManager.GetActiveScene()) continue;
            if (t.gameObject.hideFlags != HideFlags.None) continue;
            if (t.name == "escape_ship") { ship = t; break; }
        }

        if (ship != null)
        {
            var p = ship.position;
            return new Vector3(p.x + 220f, p.y + 30f, p.z);
        }
        return new Vector3(220f, 30f, 0f);
    }

    public static void Reveal()
    {
        if (instance != null) instance.DoReveal();
    }

    public static void Retreat()
    {
        if (instance != null) instance.targetAlpha = 0f;
    }

    void DoReveal()
    {
        if (revealed) return;
        revealed = true;
        targetAlpha = 1f;
        SubtitleManager.Show("[smoke rises on the eastern horizon]", 3.5f);
    }

    void Start()
    {
        BuildQuad();
        if (revealOnStart) DoReveal();
        StartCoroutine(PeriodicKeyHook());
    }

    IEnumerator PeriodicKeyHook()
    {
        while (!revealed)
        {
            TryHookKey();
            yield return new WaitForSeconds(1.5f);
        }
    }

    void TryHookKey()
    {
        var all = FindObjectsByType<XRBaseInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ia in all)
        {
            if (ia == null || hooked.Contains(ia)) continue;
            if (!ia.name.Contains("Key_RockDoor")) continue;
            hooked.Add(ia);
            ia.selectEntered.AddListener(OnKeyGrabbed);
        }
    }

    void OnKeyGrabbed(SelectEnterEventArgs _)
    {
        if (!revealed) DoReveal();
    }

    void BuildQuad()
    {
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "HorizonSmokeQuad";

        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        if (Vector3.Distance(transform.position, Vector3.zero) > 0.1f)
            quad.transform.LookAt(Vector3.zero);

        basePos = quad.transform.localPosition;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        mat = new Material(shader);
        mat.mainTexture = BuildTexture(512, 256);

        var c = smokeColor; c.a = 0f;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;

        var r = quad.GetComponent<MeshRenderer>();
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    Texture2D BuildTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pix = new Color32[w * h];

        float[] centers = { 0.24f, 0.50f, 0.76f };
        float[] widths  = { 0.14f, 0.20f, 0.17f };
        float[] heights = { 0.68f, 1.00f, 0.82f };

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float v = y / (float)h;

            float a = 0f;
            for (int i = 0; i < centers.Length; i++)
            {
                float dx = (u - centers[i]) / widths[i];
                float plume = Mathf.Exp(-(dx * dx));
                float risiness = Mathf.SmoothStep(0f, 1f, v / heights[i]);
                float fall = 1f - risiness * 0.85f;
                a += plume * fall;
            }
            a = Mathf.Clamp01(a);

            float noise = 0.85f + 0.15f * Mathf.PerlinNoise(u * 8f, v * 4f);
            a *= noise;

            float topFade = 1f - Mathf.SmoothStep(0.65f, 1f, v);
            a *= topFade;

            byte alpha = (byte)Mathf.Clamp(a * 255f, 0f, 255f);
            pix[y * w + x] = new Color32(0, 0, 0, alpha);
        }

        tex.SetPixels32(pix);
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (mat == null || quad == null) return;

        float rate = Mathf.Max(0.1f, fadeInSeconds);
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / rate);
        var c = mat.color; c.a = currentAlpha;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;

        phaseX += Time.deltaTime * driftSpeed;
        phaseY += Time.deltaTime * driftSpeed * 0.7f;
        float dx = Mathf.Sin(phaseX) * driftAmplitude * 0.5f;
        float dy = Mathf.Sin(phaseY + 1.1f) * driftAmplitude;
        quad.transform.localPosition = basePos + new Vector3(dx, dy, 0f);
    }
}
