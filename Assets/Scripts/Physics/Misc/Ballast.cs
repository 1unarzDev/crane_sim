using UnityEngine;

public class Ballast : MonoBehaviour
{
    [SerializeField] private float mass = 30.0f;
    private Rigidbody rb;
    const float g = 9.8067f;
    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        rb.AddForceAtPosition(Vector3.down * g * mass, transform.position);
    }
}
