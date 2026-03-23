using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;


public class PlayerController : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    [Space]

    [Header("Movement")]
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 4f;
    [SerializeField, Range(0f, 100f)] private float runMultiplier = 4f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 35f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 20f;
    [SerializeField, Range(0f, 100f)] private float maxCrouchAcceleration = 20f;
    
    
    private InputAction run;
    [Header("Crouching")]
    [SerializeField] private Collider2D CrouchCollider;
    [SerializeField] private Vector2 ceelingOffset;
    private bool crouching = false;
    private bool wantsToCrouch;
    private InputAction crouch;

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
    [SerializeField, Range(0f, 1f)] private float coyoteTime = 0.2f;
    private float coyoteTimeTimer;
    [SerializeField, Range(0f, 1f)] private float jumpBufferTime = 0.2f;
    private float jumpBufferTimer;
    private bool jumped;
    private InputAction jump;
    private int jumpPhase;
    private float defaultGravityScale;

    [Header("Wall jumping")]
    [SerializeField, Range(0f, 100f)] private float slideSpeed = 10;
    [SerializeField, Range(0f, 100f)] private float wallJumpLerp = 10;
    private bool onRightWall, onLeftWall;
    private bool desiredJump;

    [Header("Ledge grabbing")]
    [SerializeField] private Vector2 rightLedgeDetector, leftLedgeDetector;
    [SerializeField, Range(0f, 1f)] private float ledgeCollisionRadius = 0.25f;
    private bool leftLedgeDetected = false, rightLedgeDetected = false;
    private bool canDetect;

    [SerializeField] private Vector2 ledgeOffset1, ledgeOffset2;
    private Vector2 climbBeginPos, climbOverPos;

    private bool canGrabLedge = true, canClimb;

    [Header("Fighting")]
    [SerializeField] private Vector2 attackPoint;
    [SerializeField, Range(0f, 1f)] private float attackRadius;
    [SerializeField] private LayerMask enemyLayer;

    private bool groundPound;

    private InputAction point;
    private InputAction attack;

    [Header("Animations")]
    [SerializeField] private Animator anim;
    private int face = 1;

    private Rigidbody2D rb;
    private PlayerInputAction playerControls;


    private void OnEnable()
    {
        attack = playerControls.Player.Attack;
        point = playerControls.Player.Look;
        attack.performed += Attack;
        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        run = playerControls.Player.Sprint;
        crouch = playerControls.Player.Crouch;
        point.Enable();
        crouch.Enable();
        run.Enable();
        jump.Enable();
        attack.Enable();
        move.Enable();
    }
    private void OnDisable()
    {
        attack.Disable();
        crouch.Disable();
        run.Disable();
        jump.Disable();
        move.Disable();
        point.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerControls = new PlayerInputAction();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
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
        //ledge detection
        if (canDetect && !crouching)
        {
            leftLedgeDetected = Physics2D.OverlapCircle((Vector2)transform.position + leftLedgeDetector, ledgeCollisionRadius, groundLayer);
            rightLedgeDetected =  Physics2D.OverlapCircle((Vector2)transform.position + rightLedgeDetector, ledgeCollisionRadius, groundLayer);

        }

        desiredJump = jump.IsPressed();
        wantsToCrouch = crouch.IsPressed();
        direction = move.ReadValue<Vector2>();
        desiredVelocity = new Vector2(direction.x, 0f) * maxSpeed;
        SetAttackPoint();
        if (run.IsPressed() && !crouching)
        {
            desiredVelocity *= runMultiplier;

        }
        CheckForLedge();
    }
    private void FixedUpdate()
    {
        velocity = rb.linearVelocity;
        if(!groundPound)
            Move();
        GroundPound();
        #region jumping
        if (jump.ReadValue<float>() == 0)
            jumped = false;
        if (!jumped && velocity.y > 0)
        {
            velocity.y -= downwardMovementMultiplier* 2 * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, 0f);
        }
        if (onGround)
        {
            groundPound = false;
            jumpPhase = 0;
            coyoteTimeTimer = coyoteTime;

            velocity.x = desiredVelocity.x;

            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer = 0f;
                Jump();
            }
        }
        else
        {
            coyoteTimeTimer -= Time.deltaTime;
        }
        if(onWall && !onGround)
        {
            if(direction.x != 0)
            {
                
                WallSlide();
            }
        }
        if (desiredJump)
        {
            if (canClimb)
            {
                //system for now that I don't have an animation
                LedgeClimbOver();
                return;
            }
            desiredJump = false;
            
            if (onWall && !onGround)
            {
                WallJump();
            }
            else
            {
                Jump();
            }
            jumped = true;
            //jump buffer logic
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
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
        #endregion
        Animate();
        rb.linearVelocity = velocity;
    }
    private void Animate() { 
        if(face != Mathf.Sign(velocity.x) && velocity.x !=0)
        {
            face *= -1;
            Vector3 scale = transform.localScale;
            scale.x= face;
            transform.localScale = scale;
        }
        anim.SetBool("OnGround", onGround);
        anim.SetBool("Crouch", crouching);
        anim.SetFloat("Jump", velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(velocity.x));
    }
    //Move function
    private void Move()
    {
        //check if I can stand up again
        if (wantsToCrouch)
        {
            crouching = true;
        }
        else
        {
            if (!Physics2D.OverlapCircle((Vector2)transform.position + ceelingOffset, collisionRadius, groundLayer))
            {
                crouching = false;
            }
        }
        acceleration = onGround ?  crouching ?  maxCrouchAcceleration : maxAcceleration : maxAirAcceleration;
        if (crouching)
        {

            CrouchCollider.enabled = false;
        }
        else
        {
            CrouchCollider.enabled = true;
        }
        maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
    }
    //jump function
    private void Jump()
    {
        if(onGround || jumpPhase < maxAirJumps||coyoteTimeTimer >0)
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
        if ((onWall || jumpPhase < maxAirJumps) && !onGround && !jumped)
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
        velocity.y = Mathf.Max(velocity.y, -slideSpeed);
    }
    //display gorund detect points
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        var positions = new Vector2[] { bottomOffset, rightOffset, leftOffset };

        Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + attackPoint, attackRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + ceelingOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightLedgeDetector, ledgeCollisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftLedgeDetector, ledgeCollisionRadius);
    }

    //ledge grab system
    private void CheckForLedge()
    {
        if(leftLedgeDetected && canGrabLedge)
        {
            canGrabLedge = false;

            climbBeginPos = (Vector2)this.transform.position + ledgeOffset1;
            climbOverPos = (Vector2)this.transform.position +new Vector2(-ledgeOffset2.x, ledgeOffset2.y);

            canClimb = true;

        }
        else if(rightLedgeDetected && canGrabLedge)
        {
            canGrabLedge = false;


            climbBeginPos = (Vector2)this.transform.position + ledgeOffset1;
            climbOverPos = (Vector2)this.transform.position + ledgeOffset2;

            canClimb = true;
        }

        if (canClimb)
        {
            transform.position = climbBeginPos;
        }
    }
    private void LedgeClimbOver()
    {
        canClimb = false;
        transform.position = climbOverPos;
        canGrabLedge = true;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) canDetect = false;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) canDetect = true;
    }

    //attacking
    private void Attack(InputAction.CallbackContext ctx)
    {

        if (onGround)
        {
            if(Physics2D.OverlapCircle((Vector2)transform.position+attackPoint, attackRadius, enemyLayer))
            {
                Debug.Log("Atak");
            }
        }
        else if(!onGround && attackPoint.y <0)
        {
            //groundPound
            groundPound = true;
        }
        
        
    }
    private void SetAttackPoint()
    {
        if (move.ReadValue<Vector2>().x < 0f )
            attackPoint = new Vector2(-1, 0);
        else if (move.ReadValue<Vector2>().y < 0f)
            attackPoint = new Vector2(0, -1);
        else if (move.ReadValue<Vector2>().y > 0f)
            attackPoint = new Vector2(0, 1);
        else 
            attackPoint = new Vector2(1, 0);
    }
    private void GroundPound()
    {
        if (groundPound)
        {
            velocity.y -= downwardMovementMultiplier*100 * Time.deltaTime;
            if (Physics2D.OverlapCircle((Vector2)transform.position + attackPoint, attackRadius, enemyLayer))
            {
                Debug.Log("Atak");
            }
        }
    }
}
