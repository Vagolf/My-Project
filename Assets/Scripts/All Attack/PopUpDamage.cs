using UnityEngine;
using UnityEngine.Rendering;

public class PopUpDamage : MonoBehaviour
{
    public Vector2 InitiaVelocity;
    public Rigidbody2D rb;
    public float lifeTime = 1f; // Time before the popup is destroyed

    // Start is called before the first frame update
    void Start()
    {
        rb.velocity = InitiaVelocity;
        Destroy(gameObject, lifeTime); // Destroy the popup after 1 second
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
