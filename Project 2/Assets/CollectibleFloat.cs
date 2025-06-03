using UnityEngine;

public class CollectibleFloat : MonoBehaviour
{
    public float speed = 2f;
    public float height = 0.5f;

    private Vector3 start_position;

    private void Start()
    {
        start_position = transform.position;
    }

    private void Update()
    {
        float new_y = start_position.y + Mathf.Sin(Time.time * speed) * height;
        transform.position = new Vector3(transform.position.x, new_y, transform.position.z);
    }
}
