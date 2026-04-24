using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DistantIronclads : MonoBehaviour
{
    static DistantIronclads instance;

    [Header("Placement relative to HorizonSmoke")]
    public Vector3 fallbackPosition = new Vector3(0f, 18f, 150f);
    public Vector2 size             = new Vector2(140f, 20f);

    [Header("Reveal")]
    public float fadeInSeconds = 3.5f;
    public Color hullColor     = new Color(0.08f, 0.08f, 0.09f, 1f);

    [Header("Drift")]
    public float driftAmplitude = 0.8f;
    public float driftSpeed     = 0.12f;

    GameObject quad;
    Material   mat;
    Vector3    basePos;
    float      phase;
    float      currentAlpha, targetAlpha;
    bool       revealed;

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
        if (string.IsNullOrEmpty(scene.name)) return;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;

        if (instance != null) return;
        var existing = FindAnyObjectByType<DistantIronclads>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[DistantIronclads]");
        instance = go.AddComponent<DistantIronclads>();
    }

    void Start()
    {
        AlignToSmoke();
        BuildQuad();
        StartCoroutine(PeriodicKeyHook());
    }

    void AlignToSmoke()
    {

        var smoke = FindAnyObjectByType<HorizonSmoke>();
        if (smoke != null)
        {
            var sp = smoke.transform.position;
            transform.position = new Vector3(sp.x, sp.y - 18f, sp.z);
        }
        else
        {
            transform.position = fallbackPosition;
        }
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
        if (revealed) return;
        revealed    = true;
        targetAlpha = 1f;
    }

    public static void Retreat()
    {
        if (instance != null) instance.targetAlpha = 0f;
    }

    void BuildQuad()
    {
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "IroncladsQuad";

        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale    = new Vector3(size.x, size.y, 1f);

        if (Vector3.Distance(transform.position, Vector3.zero) > 0.1f)
            quad.transform.LookAt(Vector3.zero);

        basePos = quad.transform.localPosition;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        mat = new Material(shader);
        mat.mainTexture = BuildTexture(512, 96);

        var c = hullColor; c.a = 0f;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;

        var r = quad.GetComponent<MeshRenderer>();
        r.material         = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows   = false;
    }

    Texture2D BuildTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pix = new Color32[w * h];

        float[] hullCenters = { 0.24f, 0.50f, 0.76f };
        float[] hullWidths  = { 0.11f, 0.15f, 0.12f };
        float[] hullHeight  = { 0.22f, 0.28f, 0.24f };
        float[] funnelX     = { 0.24f, 0.50f, 0.76f };
        float[] funnelW     = { 0.015f, 0.020f, 0.017f };
        float[] funnelTop   = { 0.55f, 0.70f, 0.60f };

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            float a = 0f;

            for (int i = 0; i < hullCenters.Length; i++)
            {
                if (v > hullHeight[i]) continue;
                float dx = Mathf.Abs(u - hullCenters[i]) / hullWidths[i];
                float soft = 1f - Mathf.SmoothStep(0.85f, 1.0f, dx);
                if (soft <= 0f) continue;
                float topSoft = 1f - Mathf.SmoothStep(hullHeight[i] - 0.03f, hullHeight[i], v);
                a = Mathf.Max(a, soft * topSoft);
            }

            for (int i = 0; i < funnelX.Length; i++)
            {
                if (v < hullHeight[i] || v > funnelTop[i]) continue;
                float dx = Mathf.Abs(u - funnelX[i]) / funnelW[i];
                float soft = 1f - Mathf.SmoothStep(0.8f, 1.0f, dx);
                if (soft <= 0f) continue;
                a = Mathf.Max(a, soft);
            }

            float noise = 0.92f + 0.08f * Mathf.PerlinNoise(u * 12f, v * 6f);
            a *= noise;

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

        phase += Time.deltaTime * driftSpeed;
        float dy = Mathf.Sin(phase) * driftAmplitude * 0.15f;
        quad.transform.localPosition = basePos + new Vector3(0f, dy, 0f);
    }
}
