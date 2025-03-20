using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{

    private Rigidbody2D rb;
    [SerializeField] private float speed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float friction;
    [SerializeField] private RectTransform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float climbSpeed;
    private Collider2D currentLadder; 
    private Vector3 ladderCenter;
    private float moveInput;
    private bool isGrounded;
    private bool isClimbing;
    private bool canClimb;
    private bool atLadderTop;
    private bool atLadderBottom;
    private bool isSnappingToLadder;

    private List<Collider2D> platforms;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        if (canClimb)
        {
            ladderCenter = new Vector3(currentLadder.transform.position.x, transform.position.y, transform.position.z);
        }
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
            if (!isClimbing)
            {
                ToggleGroundCollisions(false);
                StartCoroutine(SmoothMoveToLadder(ladderCenter));
            }
            
            isClimbing = true;

            rb.gravityScale = 0;
        }
        else if ((isClimbing && !isGrounded && canClimb) || (isClimbing && canClimb && input.x == 0))
        {
            moveInput = input.y;
        }
        else
        {
            moveInput = input.x;
            if (isClimbing)
            {
                ToggleGroundCollisions(true);
            }
            isClimbing = false;

            rb.gravityScale = 1;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            canClimb = true;
            currentLadder = other;
        }
        if (other.CompareTag("LadderTop"))
        {
            atLadderTop = true;
        }
        if (other.CompareTag("LadderBottom"))
        {
            atLadderBottom = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            canClimb = false;
        }
        if (other.CompareTag("LadderTop"))
        {
            atLadderTop = false;
        }
        if (other.CompareTag("LadderBottom"))
        {
            atLadderBottom = false;
        }
    }

    private void FixedUpdate()
    {
        var colliders = new List<Collider2D>();
        rb.GetContacts(colliders);

        if (isClimbing)
        {
            var ladderTop = colliders.Find(x => x.CompareTag("LadderTop"));
            if (atLadderTop && moveInput > 0 && ladderTop != null && ladderTop.transform.position.y <= transform.position.y - transform.lossyScale.y / 2)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else if (atLadderBottom && moveInput < 0)
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

    private void ToggleGroundCollisions(bool toggle)
    {
        if (platforms == null)
        {
            platforms = new List<Collider2D>();
            var objs = GameObject.FindGameObjectsWithTag("Platform");
            foreach (var obj in objs)
            {
                var c2d = obj.GetComponent<Collider2D>();
                if (c2d != null)
                {
                    platforms.Add(c2d);
                }
            }
        }

        foreach (var contact in platforms)
        {
            contact.excludeLayers = !toggle ? LayerMask.GetMask("Player") : LayerMask.GetMask();
        }
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Ground"), !toggle);
    }

    private IEnumerator SmoothMoveToLadder(Vector3 targetPosition)
    {
        isSnappingToLadder = true;
        float duration = 0.2f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        isSnappingToLadder = false;
    }
}
