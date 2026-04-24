using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmbientSeagulls : MonoBehaviour
{
    static AmbientSeagulls instance;

    AudioSource src;
    AudioClip[] calls;

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
        var existing = FindAnyObjectByType<AmbientSeagulls>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[AmbientSeagulls]");
        instance = go.AddComponent<AmbientSeagulls>();
    }

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume       = 0.18f;
        src.playOnAwake  = false;
        src.loop         = false;

        calls = new AudioClip[] {
            BuildCall(0.18f, 1200f, 1700f, 1100f, 1500f),
            BuildCall(0.24f, 900f,  1300f, 1600f, 1000f),
            BuildCall(0.16f, 1500f, 1100f, 1800f, 1200f),
            BuildCall(0.22f, 1050f, 1450f, 950f,  1350f),
        };

        StartCoroutine(CallLoop());
    }

    IEnumerator CallLoop()
    {
        yield return new WaitForSeconds(Random.Range(8f, 14f));
        while (true)
        {
            if (src != null && calls != null && calls.Length > 0)
            {
                var clip = calls[Random.Range(0, calls.Length)];
                src.pitch = Random.Range(0.9f, 1.15f);
                src.PlayOneShot(clip);

                if (Random.value < 0.35f)
                {
                    yield return new WaitForSeconds(Random.Range(0.35f, 0.7f));
                    var clip2 = calls[Random.Range(0, calls.Length)];
                    src.pitch = Random.Range(0.85f, 1.2f);
                    src.PlayOneShot(clip2);
                }
            }
            yield return new WaitForSeconds(Random.Range(14f, 32f));
        }
    }

    static AudioClip BuildCall(float duration, float f1Start, float f1End, float f2Start, float f2End)
    {
        int   rate    = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        int   count   = Mathf.CeilToInt(rate * duration);
        var   samples = new float[count];

        int gapStart = Mathf.RoundToInt(count * 0.42f);
        int gapEnd   = Mathf.RoundToInt(count * 0.52f);

        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            if (i >= gapStart && i < gapEnd)
            {
                samples[i] = 0f;
                continue;
            }

            float t    = i / (float)rate;
            float norm = i < gapStart
                       ? i / (float)gapStart
                       : (i - gapEnd) / (float)(count - gapEnd);
            norm = Mathf.Clamp01(norm);

            float f = i < gapStart
                    ? Mathf.Lerp(f1Start, f1End, norm)
                    : Mathf.Lerp(f2Start, f2End, norm);

            phase += 2f * Mathf.PI * f / rate;

            float attack  = Mathf.SmoothStep(0f, 1f, norm / 0.15f);
            float release = 1f - Mathf.SmoothStep(0.7f, 1f, norm);
            float env     = attack * release;

            float s = Mathf.Sin(phase) * 0.55f + Mathf.Sin(phase * 2.01f) * 0.22f;
            samples[i] = s * env * 0.7f;
        }

        var clip = AudioClip.Create($"Gull_{f1Start}_{f2End}", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
