using UnityEngine;

public class ShipMover : MonoBehaviour
{
    [Tooltip("Units per second")]
    public float speed = 2f;

    [Tooltip("How far the ship travels before teleporting back to origin")]
    public float loopDistance = 80f;

    [Tooltip("Gentle side-to-side rock amplitude in degrees")]
    public float rockAmplitude = 1.5f;

    [Tooltip("Rock frequency in cycles per second")]
    public float rockFrequency = 0.4f;

    Vector3 _startPos;
    Quaternion _startRot;
    float _travelled;
    float _rockPhase;

    void Start()
    {
        _startPos = transform.position;
        _startRot = transform.rotation;
        _rockPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        float step = speed * dt;
        transform.position += transform.forward * step;
        _travelled += step;

        float roll = Mathf.Sin(Time.time * rockFrequency * Mathf.PI * 2f + _rockPhase) * rockAmplitude;
        transform.rotation = _startRot * Quaternion.Euler(0f, 0f, roll);

        if (_travelled >= loopDistance)
        {
            transform.position = _startPos;
            _travelled = 0f;
        }
    }
}
