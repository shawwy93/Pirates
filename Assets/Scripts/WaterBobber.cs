using UnityEngine;

public class WaterBobber : MonoBehaviour
{
    [SerializeField] float bobHeight = 0.06f;
    [SerializeField] float bobSpeed  = 0.8f;
    [SerializeField] float tiltAngle = 3f;

    float _waterlineY;
    float _timeOffset;
    float _tiltOffset;

    void Start()
    {
        _waterlineY  = transform.position.y;
        _timeOffset  = Random.Range(0f, Mathf.PI * 2f);
        _tiltOffset  = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {

        var rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic && rb.linearVelocity.sqrMagnitude > 0.5f)
            return;

        float t   = Time.time * bobSpeed;
        float bob = Mathf.Sin(t + _timeOffset) * bobHeight;

        var pos   = transform.position;
        pos.y     = _waterlineY + bob;
        transform.position = pos;

        float rollZ  = Mathf.Sin(t * 1.3f + _tiltOffset) * tiltAngle;
        float rollX  = Mathf.Sin(t * 0.7f + _tiltOffset + 1f) * tiltAngle * 0.5f;
        transform.rotation = Quaternion.Euler(rollX, transform.rotation.eulerAngles.y, rollZ);
    }
}
