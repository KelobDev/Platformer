using System.Xml.Serialization;
using UnityEngine;

public class BulletController : MonoBehaviour
{

    [SerializeField] private int direction = 1;
    [SerializeField, Range(0f, 100f)] private float speed = 2f;
    private Vector2 movement;
    void Start()
    {
        
    }
    public void Setup(int dir)
    {
        direction = dir;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        movement = transform.position;
        movement.x  += speed * direction *Time.deltaTime;
        transform.position = movement;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.gameObject.GetComponent<PlayerController>().TakeDamage(1);
        Destroy(this.gameObject);
    }

}
