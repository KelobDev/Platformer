using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


public class PlayerController : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Movement")]
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 4f;
    [SerializeField, Range(0f, 100f)] private float runMultiplier = 4f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 35f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 20f;
    [SerializeField, Range(0f, 100f)] private float maxCrouchAcceleration = 20f;
    private InputAction run;
    private  InputAction move;
    private Vector2 direction;
    private Vector2 desiredVelocity;
    private Vector2 velocity;
    private float maxSpeedChange;
    private float acceleration;
    
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

    [Header("Jumping")]
    [SerializeField, Range(0f, 10f)] private float jumpHeight = 3f;
    [SerializeField, Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField, Range(0f, 5f)] private float downwardMovementMultiplier = 3f;
    [SerializeField, Range(0f, 5f)] private float upwardMovementMultiplier = 1.7f;
    [SerializeField, Range(0f, 1f)] private float coyoteTime = 0.2f;
    [SerializeField, Range(0f, 1f)] private float jumpBufferTime = 0.2f;
    private float coyoteTimeTimer;
    private float jumpBufferTimer;
    private bool jumped;
    private InputAction jump;
    private int jumpPhase;
    private float defaultGravityScale;

    [Header("Wall jumping")]
    [SerializeField, Range(0f, 100f)] private float slideSpeed = 10;
    [SerializeField, Range(0f, 100f)] private float wallJumpLerp = 10;
    [SerializeField, Range(0f, 10f)] private float wallJumpHeight = 3f;
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
    [SerializeField] private Tilemap destructables;//for destroying elemmts by punching
    private bool groundPound;
    private InputAction attack;
    //shooting
    private float holdTime;
    private bool isCharging = false;
    [SerializeField] private float minHoldDuration = 0.5f;
    [SerializeField, Range(0f,100f)] private float headJumpForce = 10f;
    [SerializeField] private GameObject bulletPrefab;



    [Header("Animations")]
    [SerializeField] private Animator anim;
    public int face = 1;

    [Header("Camera system")]
    [SerializeField] private GameObject cameraFollowObj;
    private CameraFollow camFollow;
    private float fallSpeedYDampingChangeThreshold;

    [Header("Respawn")]
    [SerializeField] private int health;
    [SerializeField] private GameObject checkPoint;

    private Rigidbody2D rb;
    private PlayerInputAction playerControls;

    #region Setup Inputs
    private void OnEnable()
    {
        attack = playerControls.Player.Attack;
        attack.performed += Attack;
        attack.started += StartCharging;
        attack.canceled += RelaseAttack;
        move = playerControls.Player.Move;
        jump = playerControls.Player.Jump;
        run = playerControls.Player.Sprint;
        crouch = playerControls.Player.Crouch;
        crouch.Enable();
        run.Enable();
        jump.Enable();
        attack.Enable();
        move.Enable();
    }
    private void OnDisable()
    {
        attack.performed -= Attack;
        attack.started -= StartCharging;
        attack.canceled -= RelaseAttack;
        attack.Disable();
        crouch.Disable();
        run.Disable();
        jump.Disable();
        move.Disable();
    }
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerControls = new PlayerInputAction();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultGravityScale = 1f;

        if (cameraFollowObj != null)
        {
            camFollow = cameraFollowObj.GetComponent<CameraFollow>();
        }
        else
        {
            Debug.LogError("Brak cameraFollowObj!");
        }

        if (CameraManager.instance != null)
        {
            fallSpeedYDampingChangeThreshold = CameraManager.instance.fallSpeedYDampingChangeThreshold;
        }
        else
        {
            Debug.LogError("Brak CameraManager.instance!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        #region collision system
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
        #endregion

        #region input system
        desiredJump = jump.IsPressed();
        wantsToCrouch = crouch.IsPressed();
        direction = move.ReadValue<Vector2>();
        desiredVelocity = new Vector2(direction.x, 0f) * maxSpeed;
        SetAttackPoint();
        if (run.IsPressed() && !crouching)
        {
            desiredVelocity *= runMultiplier;

        }
        #endregion
        CheckForLedge();
        #region camera system
        //update camera
        if (rb.linearVelocity.y < fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpYDamping(true);
        }
        if(rb.linearVelocity.y >=0f && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpedFromPlayerFalling = false;
            CameraManager.instance.LerpYDamping(false);
        }
        #endregion
    }
    private void FixedUpdate()
    {

        velocity = rb.linearVelocity;//get players velocity to work on it
        if(!groundPound)
            Move();
        GroundPound();
        HeadJump();
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
            groundPound = false;//can't ground pound anymore
            jumpPhase = 0;
            coyoteTimeTimer = coyoteTime;//reset coyoteTimer

            velocity.x = desiredVelocity.x;

            //if we just landed and wanted to jump
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
        if(face != Mathf.Sign(velocity.x) && velocity.x !=0 && canGrabLedge)
        {
            face *= -1;
            Vector3 scale = transform.localScale;
            scale.x= face;
            transform.localScale = scale;
            camFollow.CallTurn();
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
    #region wall jump
    private void WallJump()
    {
        if ((onWall || jumpPhase < maxAirJumps) && !onGround && !jumped)
        {
            int wallDir = onRightWall ? -1 : 1;
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * wallJumpHeight);
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0);
            }
            velocity.y += jumpSpeed;
            velocity.x += wallDir * wallJumpLerp; 

        }

    }
    private void WallSlide()
    {
        bool pushingWall = (velocity.x== 0 && onRightWall) || (velocity.x == 0 && onLeftWall);

        float push = pushingWall ? 0 : velocity.x;
        velocity.y = Mathf.Max(velocity.y, -slideSpeed);
    }
    #endregion
    //ledge grab system
    #region ledge grab
    private void CheckForLedge()
    {

        int dir = rightLedgeDetected ? 1 : -1;

        Vector2 detector = rightLedgeDetected ? rightLedgeDetector : leftLedgeDetector;

        //check if we're not trying to climb on ceeling
        bool notUnderGround = !Physics2D.OverlapCircle((Vector2)transform.position + ceelingOffset, collisionRadius, groundLayer);
        if ((leftLedgeDetected || rightLedgeDetected) && canGrabLedge)
        {
            
            canGrabLedge = false;
            
            climbBeginPos = (Vector2)transform.position + detector + new Vector2(ledgeOffset1.x * dir, ledgeOffset1.y);
            climbOverPos = (Vector2)transform.position + detector + new Vector2(ledgeOffset2.x * dir, ledgeOffset2.y);


            if (!Physics2D.OverlapCircle(climbOverPos, collisionRadius, groundLayer) && notUnderGround)
            {
                canGrabLedge = true;
                return;
            }
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
        velocity= new Vector2 (0,0);
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
    #endregion
    //Health system
    #region healthSystem
    private void Die()
    {
        transform.position = checkPoint.transform.position;
        health = 1;
    }
    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            Die();
    }
    public void SetCheckpoint(GameObject ncp)
    {
        checkPoint.GetComponent<Checkpoint>().taken = false;
        checkPoint = ncp;
        checkPoint.GetComponent<Checkpoint>().taken = true;
    }
    #endregion
    //attacking
    #region Attack
    private void HeadJump()
    {
        if(Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, enemyLayer))
        {
            //kill enemy we jumped on
            Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + bottomOffset, attackRadius, enemyLayer);
            //fight with enemies
            foreach (var hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(1);
                    velocity.y = headJumpForce;
                }
            }
        }
    }
    private void Attack(InputAction.CallbackContext ctx)
    {

        if (onGround)
        {
            ExectueAttack();
        }
        else if(!onGround && attackPoint.y <0)
        {
            //groundPound
            groundPound = true;
        }
        
        
    }
    void ExectueAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + attackPoint, attackRadius, enemyLayer);
        //fight with enemies
        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
                enemy.TakeDamage(1);
        }

        //destroy objects
        Vector2 center = (Vector2)transform.position + attackPoint;
        //iloœæ sprawdzanych punktów
        int checks = 20;

        for (int i = 0; i < checks; i++)
        {
            float angle = i * Mathf.PI * 2 / checks;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 point = center + dir * attackRadius;

            Vector3Int cellPos = destructables.WorldToCell(point);
            //sprawcamy czy nasz kafelek znajduje siê w zasiêgu punktu ataku
            if (destructables.HasTile(cellPos))
            {
                destructables.SetTile(cellPos, null);
                Debug.Log("Puk puk puk puk");
            }
        }
    }
    private void StartCharging(InputAction.CallbackContext ctx)
    {
        isCharging = true;
        holdTime = (float)ctx.startTime;//save start time of holding
        Debug.Log("Charging animation here");
    }
    private void RelaseAttack(InputAction.CallbackContext ctx) { 
    
        float duration = (float)(ctx.time - holdTime);
        if (isCharging)
        {
            if(duration >= minHoldDuration)
            {
                
                //spawn bullet
                bulletPrefab.GetComponent<BulletController>().Setup(face);
                bulletPrefab.transform.position = new Vector3(transform.position.x + attackPoint.x, transform.position.y + attackPoint.y,0);
                Instantiate(bulletPrefab);
            }
            else
            {
                Debug.Log("zbyt krotko");
            }
            isCharging=false;
            //turn off charigng animation
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
        else if(move.ReadValue<Vector2>().x > 0f)
            attackPoint = new Vector2(1, 0);
    }
    private void GroundPound()
    {
        if (groundPound)
        {
            //don't go over sppeed limit to prevent being ground stuck
            if (velocity.y > -60)
            {
                velocity.y -= downwardMovementMultiplier*100 * Time.deltaTime;
                
            }
            ExectueAttack();
        }
    }
    #endregion

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
        Gizmos.DrawWireSphere(climbOverPos, collisionRadius);//debugging for climbing ledges
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            TakeDamage(1);
        }
    }
}
