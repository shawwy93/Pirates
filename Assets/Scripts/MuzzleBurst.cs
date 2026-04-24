using System.Collections;
using UnityEngine;

public class MuzzleBurst : MonoBehaviour
{
    public static void Spawn(Vector3 position, Quaternion rotation)
    {
        var go = new GameObject("MuzzleBurst");
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.AddComponent<MuzzleBurst>();
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        BuildFlash();
        BuildSmokeCloud();

        yield return new WaitForSeconds(2.2f);
        Destroy(gameObject);
    }

    void BuildFlash()
    {
        var flash = new GameObject("Flash");
        flash.transform.SetParent(transform, false);
        flash.transform.localPosition = Vector3.forward * 0.15f;

        var light = flash.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = new Color(1f, 0.55f, 0.15f);
        light.intensity = 12f;
        light.range     = 8f;

        flash.AddComponent<FlashDecay>();

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(flash.transform, false);
        quad.transform.localScale = Vector3.one * 0.9f;
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        var mat = new Material(shader);
        mat.mainTexture = BuildRadialTex(128, new Color(1f, 0.7f, 0.25f));
        mat.color       = new Color(1f, 0.85f, 0.4f, 1f);

        var r = quad.GetComponent<MeshRenderer>();
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows    = false;
    }

    void BuildSmokeCloud()
    {
        var cloud = new GameObject("Smoke");
        cloud.transform.SetParent(transform, false);
        cloud.transform.localPosition = Vector3.forward * 0.4f;
        cloud.AddComponent<SmokeBillow>();
    }

    Texture2D BuildRadialTex(int size, Color core)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pix = new Color32[size * size];
        float r2 = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - r2) / r2;
            float dy = (y - r2) / r2;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float a  = Mathf.Clamp01(1f - d);
            a = a * a;
            pix[y * size + x] = new Color32(
                (byte)(core.r * 255f),
                (byte)(core.g * 255f),
                (byte)(core.b * 255f),
                (byte)(a * 255f));
        }
        tex.SetPixels32(pix);
        tex.Apply();
        return tex;
    }

    class FlashDecay : MonoBehaviour
    {
        Light  flashLight;
        MeshRenderer mr;
        Color  baseColor;
        float  elapsed;
        float  duration = 0.18f;

        void Start()
        {
            flashLight = GetComponent<Light>();
            mr    = GetComponentInChildren<MeshRenderer>();
            if (mr != null) baseColor = mr.material.color;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float k = 1f - t;
            if (flashLight != null) flashLight.intensity = 12f * k * k;
            if (mr != null)
            {
                var c = baseColor;
                c.a   = k;
                mr.material.color = c;
            }
            if (t >= 1f) Destroy(gameObject);
        }
    }

    class SmokeBillow : MonoBehaviour
    {
        MeshRenderer mr;
        Material     mat;
        float        elapsed;
        float        duration = 1.8f;
        Vector3      drift;

        void Start()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = Vector3.one * 0.4f;
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            mat = new Material(shader);
            mat.mainTexture = BuildPuffTex(128);
            mat.color       = new Color(0.85f, 0.82f, 0.78f, 0.9f);

            mr = quad.GetComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            drift = new Vector3(Random.Range(-0.2f, 0.2f),
                                Random.Range(0.4f, 0.6f),
                                Random.Range(0.5f, 1.2f));
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.localScale   = Vector3.one * Mathf.Lerp(1f, 4.5f, t);
            transform.position    += drift * Time.deltaTime;

            var c = mat.color;
            c.a   = Mathf.Lerp(0.9f, 0f, t);
            mat.color = c;

            if (t >= 1f) Destroy(gameObject);
        }

        Texture2D BuildPuffTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pix = new Color32[size * size];
            float r2 = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - r2) / r2;
                float dy = (y - r2) / r2;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float soft = Mathf.Clamp01(1f - d);
                soft = Mathf.SmoothStep(0f, 1f, soft);
                float noise = 0.75f + 0.25f * Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                float a = soft * noise;
                pix[y * size + x] = new Color32(230, 225, 218, (byte)(a * 255f));
            }
            tex.SetPixels32(pix);
            tex.Apply();
            return tex;
        }
    }
}
