using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [SerializeField] float baseIntensity  = 1.4f;
    [SerializeField] float flickerAmount  = 0.7f;
    [SerializeField] float fastSpeed      = 12f;
    [SerializeField] float slowSpeed      = 2.5f;

    Light  _light;
    float  _fastOffset;
    float  _slowOffset;

    void Awake()
    {
        _light      = GetComponent<Light>();
        _fastOffset = Random.Range(0f, 64f);
        _slowOffset = Random.Range(0f, 64f);
    }

    void Update()
    {
        float fast  = Mathf.PerlinNoise(Time.time * fastSpeed, _fastOffset) - 0.5f;
        float slow  = Mathf.PerlinNoise(Time.time * slowSpeed, _slowOffset) - 0.5f;
        float noise = fast * 0.6f + slow * 0.4f;
        _light.intensity = Mathf.Max(0f, baseIntensity + noise * flickerAmount * 2f);
    }
}
