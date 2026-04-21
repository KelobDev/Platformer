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
    [SerializeField] private LayerMask playerLayer;

    [Header("Health system")]
    [SerializeField, Range(1f,2f)] private float health = 1;

    [Header("Movement system")]
    [SerializeField, Range(0f, 100f)] private float speed = 4f;
    private bool OnWall, OnCeeling,OnGround;
    [SerializeField]private Transform pt = null;
    private Vector2 velocity, direction;

    [Header("Attacking System")]
    [SerializeField] private Vector2 shootPoint;
    [SerializeField, Range(0f, 5f)] private float shootingTime = 1f;
    [SerializeField] private float shootingTimer = 0f;
    [SerializeField] private GameObject Bullet;


    [Header("Detectors")]
    [SerializeField] private Vector2 leftDetector, rightDetector, groundDetector, topDetector, LDDetector, RDDetector;
    [SerializeField, Range(0f, 1f)] private float detectorRadius;
    [SerializeField] private Vector2 playerDetector;
    [SerializeField, Range(0, 10f)] private float playerDetectorRadius;

    [Header("AI Logic")]
    private Vector2 lastPlayerPosition;
    private bool canSeePlayer;
    private enum AIState { IDLE, CHASE,SEARCH}
    [SerializeField] private AIState currentState = AIState.IDLE;
    [SerializeField] private float searchWaitTime = 2f;
    private float searchTimer;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shootingTimer = shootingTime;
        Bullet.GetComponent<BulletController>().Setup((int)shootPoint.x);
        if (type == Type.NEUTRAL)
            direction.x = 1;
        if(type == Type.WALLCRAWL)
            direction.y = 1;
        if(LevelManager.instance!=null)
            LevelManager.instance.maxPoints += 2;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (type == Type.NEUTRAL)
        {
            NeutralDirection();
        }
        if(type == Type.WALLCRAWL)
        {
            WallCrawlDirection();
        }
        if(type == Type.ENRANGED)
        {
            EnrangedDirection();
        } 
    }
    private void FixedUpdate()
    {
        if (type != Type.BASIC)
            Move();
        if (type == Type.RANGED)
            Shoot();
        if(type == Type.WALLCRAWL)
            WallCrawl();
        if (type == Type.ENRANGED)
            EnrangedMove();
    }
    //movements
    private void Move()
    {
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        if (direction.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }
    private void EnrangedMove()
    {
        //Ruch poziomy
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        //ruch pionowy
        bool isGrounded = Physics2D.OverlapCircle((Vector2)transform.position + groundDetector, detectorRadius, groundLayer);
        bool wallAhead = direction.x >0 ?
            Physics2D.OverlapCircle((Vector2)transform.position + rightDetector, detectorRadius,groundLayer) :
            Physics2D.OverlapCircle((Vector2)transform.position + leftDetector, detectorRadius, groundLayer);
        if(currentState == AIState.CHASE && isGrounded)
        {
            //skok
            if (wallAhead && lastPlayerPosition.y > transform.position.y + 1.5f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 7f); // 7f to si³a skoku, dostosuj j¹
            }
        }
        // Obrót sprite'a
        if (direction.x != 0) transform.localScale = new Vector3(direction.x > 0 ? 1 : -1, 1, 1);
    }

    private void WallCrawl()
    {
        //ruch do przodu po lokalnej OX
        transform.Translate(Vector2.right *speed * Time.deltaTime);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, detectorRadius + 0.2f, groundLayer);

        if(hit.collider != null)
        {
            transform.up = Vector3.Lerp(transform.up, hit.normal, Time.deltaTime);
        }
        else
        {
            transform.Rotate(0, 0, -90f);
        }

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
        LevelManager.instance.Addpoints(2);
        Destroy(this.gameObject);
    }
    //directions
    private void NeutralDirection()
    {
        if (Physics2D.OverlapCircle((Vector2)transform.position + rightDetector, detectorRadius, groundLayer) ||
                !Physics2D.OverlapCircle((Vector2)transform.position + RDDetector, detectorRadius, groundLayer))
            direction.x = -1;
        if (Physics2D.OverlapCircle((Vector2)transform.position + leftDetector, detectorRadius, groundLayer) ||
            !Physics2D.OverlapCircle((Vector2)transform.position + LDDetector, detectorRadius, groundLayer))
            direction.x = 1;
    }
    private void EnrangedDirection()
    {
        //check if player is in range
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, playerDetectorRadius, playerLayer);

        if(playerCol != null)
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, playerCol.transform.position, groundLayer);

            //nic nie zas³ania
            if(hit.collider == null)
            {
                canSeePlayer = true;
                lastPlayerPosition = playerCol.transform.position;
                currentState = AIState.CHASE;
            }
            else
            {
                canSeePlayer = false;
            }
        }
        else canSeePlayer = false;

        HandleStates();
    }
    private void HandleStates()
    {
        switch (currentState)
        {
            case AIState.CHASE:
                direction.x = (lastPlayerPosition.x < transform.position.x) ? -1 : 1;
                if (Mathf.Abs(lastPlayerPosition.y - transform.position.y) > 0.5f)
                    direction.y = (lastPlayerPosition.y < transform.position.y) ? -1 : 1;

                if (!canSeePlayer) currentState = AIState.SEARCH;
                break;
            case AIState.SEARCH:
                float distToLastPos = Vector2.Distance(transform.position, lastPlayerPosition);
                if(distToLastPos > 0.5f)
                {
                    direction.x = (lastPlayerPosition.x < transform.position.x) ? -1 : 1;
                }
                else
                {
                    searchTimer += Time.deltaTime;
                    direction = Vector2.zero;
                    if(searchTimer >= searchWaitTime)
                    {
                        searchTimer = 0;
                        currentState = AIState.IDLE;
                    }
                }
                break;
            case AIState.IDLE:
                if (direction.x == 0) direction.x = 1;
                //placeholder do IDLe lub finalny
                NeutralDirection();
                break;
        }
        
    }
    private void WallCrawlDirection()
    {
        if (Physics2D.OverlapCircle((Vector2)transform.position + rightDetector, detectorRadius, groundLayer) ||
                Physics2D.OverlapCircle((Vector2)transform.position + leftDetector, detectorRadius, groundLayer))
            OnWall = true;
        else OnWall = false;
        if (Physics2D.OverlapCircle((Vector2)transform.position + groundDetector, detectorRadius, groundLayer))
            OnGround = true;
        else OnGround = false;
        if (Physics2D.OverlapCircle((Vector2)transform.position + topDetector, detectorRadius, groundLayer))
            OnCeeling = true;
        else OnCeeling = false;
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
        Gizmos.DrawWireSphere((Vector2)transform.position + RDDetector, detectorRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + LDDetector, detectorRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + playerDetector, playerDetectorRadius);
    }
}
