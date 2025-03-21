using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float friction;
    [SerializeField] private float hammerDuration = 10f;
    
    [SerializeField] private RectTransform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float climbSpeed;
    private float moveInput;
    private bool isGrounded;
    private bool isClimbing;
    private bool canClimb;
    private bool atLadderTop;
    private bool atLadderBottom;

    private Vector3 baseScale = Vector3.one;
    
    public bool UsingHammer { get; private set; }

    public int facingDirection = 1;
    
    private List<Collider2D> platforms;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
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

        if (input.x != 0)
        {
            facingDirection = (int) Mathf.Sign(input.x);   
        }
        
        if (canClimb && input.y != 0)
        {
            moveInput = input.y;
            if (!isClimbing)
            {
                ToggleGroundCollisions(false);   
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
        }
        if (other.CompareTag("LadderTop"))
        {
            atLadderTop = true;
        }
        if (other.CompareTag("LadderBottom"))
        {
            atLadderBottom = true;
        }
        if (other.CompareTag("HammerObject"))
        {
            var hammerObject = other.GetComponent<HammerObject>();
            if (hammerObject == null || UsingHammer) return;
        
            hammerObject.Collect();
            StartCoroutine(UseHammer());   
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
        
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        if (!isClimbing)
        {
            transform.localScale = new Vector3(facingDirection * baseScale.x, baseScale.y, baseScale.z);   
        }
        
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

    private IEnumerator UseHammer()
    {
        UsingHammer = true;

        const float inc = 0.1f;
        float hammerTime = 0;
        
        while (UsingHammer && hammerTime <= hammerDuration)
        {
            yield return new WaitForSeconds(inc);
            hammerTime += inc;
        }

        UsingHammer = false;
    }
}
