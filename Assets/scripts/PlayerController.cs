using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    [Header("InputSystem")]
    public PlayerInputAction playerControls;

    [Header("Movement")]
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 4f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 35f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 20f;


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

    private bool desiredJump;
    private bool onGround;


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
        desiredJump = jump.ReadValue<float>()!=0;
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
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
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

    //checking if player is on ground
    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        onGround = false;//player leaves ground
    }
    private void EvaluateCollision(Collision2D collision)
    {
        for(int i =0; i<collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            onGround |= normal.y >= 0.9f;
        }
    }
}
