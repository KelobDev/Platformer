using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    [Header("InputSystem")]
    public PlayerInputAction playerControls;

    [Header("Movement")]
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 4f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 35f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 20f;

    [Header("Collisions")]
    [SerializeField, Range(0f, 1f)] private float collisionRadius = 0.25f;
    [SerializeField] private Vector2 bottomOffset, rightOffset, leftOffset;
    private bool onGround, onWall;


    private  InputAction move;

    private Vector2 direction;
    private Vector2 desiredVelocity;
    private Vector2 velocity;

    private float maxSpeedChange;
    private float acceleration;

    [Header("Jumping")]
    [SerializeField, Range(0f, 10f)] private float jumpHeight = 3f;
    [SerializeField, Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField, Range(0f, 5f)] private float downwardMovementMultiplier = 3f;
    [SerializeField, Range(0f, 5f)] private float upwardMovementMultiplier = 1.7f;
    private InputAction jump;
    private int jumpPhase;
    private float defaultGravityScale;

    [Header("Wall jumping")]
    [SerializeField, Range(0f, 100f)] private float slideSpeed = 10;
    [SerializeField, Range(0f, 100f)] private float wallJumpLerp = 10;
    private bool onRightWall, onLeftWall;
    private int wallSide;


    private bool desiredJump;


    private Rigidbody2D rb;


    private void OnEnable()
    {
        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        jump.Enable();
        move.Enable();
    }
    private void OnDisable()
    {
        jump.Disable();
        move.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerControls = new PlayerInputAction();
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        //checks if player is touching ground
        onGround = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, groundLayer);
        //checks if player is touching wall
        onWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer)
            || Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);
        onRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer);
        onLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        desiredJump = jump.IsPressed();
        direction = move.ReadValue<Vector2>();
        desiredVelocity = new Vector2(direction.x, 0f) * maxSpeed;
    }
    private void FixedUpdate()
    {
        velocity = rb.linearVelocity;

        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);


        if (onGround)
        {
            jumpPhase = 0;
        }
        if(onWall && !onGround)
        {
            if(velocity.x !=0)
            {
                
                WallSlide();
            }
        }
        if (desiredJump)
        {
            desiredJump = false;
            
            Jump();
            WallJump();
        }
        if(rb.linearVelocity.y > 0)
        {
            rb.gravityScale = upwardMovementMultiplier;
        }
        else if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = downwardMovementMultiplier;
        }
        else if(rb.linearVelocity.y == 0)
        {
            rb.gravityScale = defaultGravityScale;
        }
        rb.linearVelocity = velocity;
    }

    //jump function
    private void Jump()
    {
        if(onGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y*jumpHeight);
            if(velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0);
            }
            velocity.y += jumpSpeed;
        }
    }
    //wall interaction
    private void WallJump()
    {
        if ((onWall && !onGround) || jumpPhase < maxAirJumps)
        {
            int wallDir = onRightWall ? -1 : 1;
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * jumpHeight);
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0);
            }
            velocity.y += jumpSpeed;
            velocity.x = wallDir * wallJumpLerp; 

        }

    }
    private void WallSlide()
    {
        bool pushingWall = (velocity.x == 0 && onRightWall) || (velocity.x == 0 && onLeftWall);

        float push = pushingWall ? 0 : velocity.x;
        velocity = new Vector2(push, -slideSpeed);
    }
    //display gorund detect points
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        var positions = new Vector2[] { bottomOffset, rightOffset, leftOffset };

        Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
    }

}
