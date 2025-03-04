using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{

    private Rigidbody2D rb;
    [SerializeField] private float speed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float friction;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float climbSpeed;
    private float moveInput;
    private bool isGrounded;
    private bool isClimbing;
    private bool canClimb;
    private bool atLadderTop;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

    }

    public void OnJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        }
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();

        if (canClimb && input.y != 0)
        {
            moveInput = input.y;
            isClimbing = true;
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Ground"), true);
            rb.gravityScale = 0;
        }
        else if ((isClimbing && !isGrounded && canClimb) || (isClimbing && canClimb && input.x == 0))
        {
            moveInput = input.y;
        }
        else
        {
            moveInput = input.x;
            isClimbing = false;
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Ground"), false);

            rb.gravityScale = 1;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ladder"))
        {
            canClimb = true;
        }
        if (other.CompareTag("LadderTop"))
        {
            atLadderTop = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ladder"))
        {
            canClimb = false;
        }
        if (other.CompareTag("LadderTop"))
        {
            atLadderTop = false;
        }
    }

    void FixedUpdate()
    {
        if (isClimbing)
        {
            if (atLadderTop && moveInput > 0)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = new Vector2(0, moveInput * climbSpeed);
            }
        }
        else if (moveInput != 0)
        {
            rb.AddForce(new Vector2(moveInput * speed, 0), ForceMode2D.Force);

            if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * (1 - friction), rb.linearVelocity.y);

            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        
    }
}
