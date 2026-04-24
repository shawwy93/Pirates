using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningLine : MonoBehaviour
{
    static OpeningLine instance;

    [SerializeField] string line      = "[dawn. no gulls on the eastern line.]";
    [SerializeField] float  delay     = 3f;
    [SerializeField] float  duration  = 4f;

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

        if (instance != null)
        {
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.PlayAfterDelay());
            return;
        }

        var existing = FindAnyObjectByType<OpeningLine>();
        if (existing != null) { instance = existing; return; }

        var go = new GameObject("[OpeningLine]");
        instance = go.AddComponent<OpeningLine>();
    }

    void Start() => StartCoroutine(PlayAfterDelay());

    IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        SubtitleManager.Show(line, duration);
    }
}
