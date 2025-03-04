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
    private float moveInput;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    public void OnJump(InputValue value)
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    void FixedUpdate()
    {
        if (moveInput != 0)
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
