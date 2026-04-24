using UnityEngine;

public class LanternSwing : MonoBehaviour
{
    [SerializeField] float swingAngle = 8f;
    [SerializeField] float swingSpeed = 0.9f;
    [SerializeField] float driftSpeed = 0.3f;

    float _timeOffset;
    float _driftOffset;

    void Awake()
    {
        _timeOffset  = Random.Range(0f, Mathf.PI * 2f);
        _driftOffset = Random.Range(0f, 64f);
    }

    void Update()
    {

        float swing = Mathf.Sin(Time.time * swingSpeed + _timeOffset) * swingAngle;

        float drift = (Mathf.PerlinNoise(Time.time * driftSpeed, _driftOffset) - 0.5f)
                      * swingAngle * 0.4f;

        transform.localRotation = Quaternion.Euler(swing, 0f, drift);
    }
}
