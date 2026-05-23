using UnityEngine;

public class Arrow : MonoBehaviour
{
    private Rigidbody rb;
    void Start()
    {
        Destroy(gameObject, 6f);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }
}