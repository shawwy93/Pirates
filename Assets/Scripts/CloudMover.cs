using UnityEngine;

public class CloudMover : MonoBehaviour
{
    public float speed = 2.0f;
    public float resetX = 100f;
    public float startX = -100f;

    void Update()
    {

        transform.Translate(Vector3.right * speed * Time.deltaTime);

        if (transform.position.x > resetX)
        {
            transform.position = new Vector3(startX, transform.position.y, transform.position.z);
        }
    }
}
