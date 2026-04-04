using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

enum Type
{
    BASIC,
    NEUTRAL,
    RANGED,
    ENRANGED,
    WALLCRAWL,
    BUFF


}
public class EnemyController : MonoBehaviour
{
    [Header("Enemy type")]
    [SerializeField] private Type type;

    [Space]

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Health system")]
    [SerializeField, Range(1f,2f)] private float health = 1;

    [Header("Movement system")]
    [SerializeField, Range(0f, 100f)] private float speed = 4f;
    private Vector2 velocity, direction;

    [Header("Attacking System")]
    [SerializeField] private Vector2 shootPoint;
    [SerializeField, Range(0f, 5f)] private float shootingTime = 1f;
    [SerializeField] private float shootingTimer = 0f;
    [SerializeField] private GameObject Bullet;


    [Header("Detectors")]
    [SerializeField] private Vector2 leftDetector, rightDetector, groundDetector;
    [SerializeField, Range(0f, 1f)] private float detectorRadius;


    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shootingTimer = shootingTime;
        Bullet.GetComponent<BulletController>().Setup((int)shootPoint.x);
        if (type == Type.NEUTRAL)
            direction.x = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (type == Type.NEUTRAL)
        {
            if (Physics2D.OverlapCircle((Vector2)transform.position + rightDetector, detectorRadius, groundLayer))
                direction.x = -1;
            if (Physics2D.OverlapCircle((Vector2)transform.position + leftDetector, detectorRadius, groundLayer))
                direction.x = 1;
        }
        if(type == Type.RANGED)
        {

        }
    }
    private void FixedUpdate()
    {
        if (type != Type.BASIC)
            Move();
        if (type == Type.RANGED)
            Shoot();
    }
    private void Move()
    {
        velocity = rb.linearVelocity;
        velocity.x += direction.x * speed * Time.deltaTime;
        rb.linearVelocity = velocity;
    }
    //take damage
    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Destroy(this.gameObject);
    }

    //attack
    private void Shoot()
    {
        if (shootingTimer > 0) {
            shootingTimer -= Time.deltaTime;
        }
        else
        {
            shootingTimer = shootingTime;
            //summon bullet with direction to go
            Bullet.transform.position =  new Vector3(transform.position.x + shootPoint.x, transform.position.y + shootPoint.y, 0);
            Instantiate(Bullet);
            Debug.Log("pif paf");

        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere((Vector2)transform.position + leftDetector, detectorRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightDetector, detectorRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + groundDetector, detectorRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + shootPoint, detectorRadius);
    }
}
