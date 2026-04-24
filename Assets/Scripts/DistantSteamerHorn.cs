using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DistantSteamerHorn : MonoBehaviour
{
    static DistantSteamerHorn instance;

    AudioSource src;
    AudioClip   hornClip;

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

        var existing = FindAnyObjectByType<DistantSteamerHorn>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[DistantSteamerHorn]");
        instance = go.AddComponent<DistantSteamerHorn>();
    }

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.volume       = 0.22f;
        src.loop         = false;
        src.playOnAwake  = false;

        hornClip = BuildHorn();
        StartCoroutine(HornLoop());
    }

    IEnumerator HornLoop()
    {
        yield return new WaitForSeconds(Random.Range(20f, 35f));
        while (true)
        {
            if (src != null && hornClip != null)
                src.PlayOneShot(hornClip);
            yield return new WaitForSeconds(Random.Range(40f, 80f));
        }
    }

    AudioClip BuildHorn()
    {
        int   rate     = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
        float duration = 3.2f;
        int   count    = Mathf.CeilToInt(rate * duration);
        var   samples  = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t       = i / (float)rate;
            float attack  = Mathf.SmoothStep(0f, 1f, t / 0.6f);
            float release = 1f - Mathf.SmoothStep(0f, 1f, (t - (duration - 0.8f)) / 0.8f);
            float env     = attack * release;

            float s = Mathf.Sin(2f * Mathf.PI *  98f * t) * 0.45f
                    + Mathf.Sin(2f * Mathf.PI * 102f * t) * 0.35f
                    + Mathf.Sin(2f * Mathf.PI * 147f * t) * 0.12f;

            samples[i] = s * env * 0.6f;
        }

        var clip = AudioClip.Create("SteamerHorn", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
