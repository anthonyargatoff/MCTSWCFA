using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private static readonly string[] DeathTags =
    {
        "Barrel",
        "DonkeyKong",
        "HostileEntity",
        "Fire"
    };
    
    private const float Speed = 13f;
    private const float MaxSpeed = 3f;
    private const float Friction = 0.1f;
    private const float HammerDuration = 16f;
    private const float JumpSpeed = 5f;
    private const float ClimbSpeed = 4f;
    
    private Rigidbody2D rb;
    
    private Collider2D currentLadder; 
    private Vector3 ladderCenter;
    private float moveInput;
    
    public bool IsGrounded { get; private set; }
    public bool IsClimbing { get; private set; }
    public bool IsWalking { get; private set; }
    public bool CanClimb { get; private set; }
    public bool AtLadderTop { get; private set; }
    public bool AtLadderBottom { get; private set; }
    public bool IsDead { get; private set; }
    public bool UsingHammer { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    
    private Vector3 baseScale = Vector3.one;
    private List<Collider2D> platforms;

    private float lethalFall = 3f;
    private float jumpPeakY;
    private bool isMidair;

    private Camera mainCamera;

    private MovingPlatform movingPlatform;
    public UnityEvent<MovingPlatform> OnEnterMovingPlatform { get; private set; } = new();
    public UnityEvent<MovingPlatform> OnExitMovingPlatform { get; private set; } = new();

    public delegate void OnDeathDelegate();
    public event OnDeathDelegate OnDeath;
    
    public delegate void OnJumpDelegate();
    public event OnJumpDelegate OnJumped;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        jumpPeakY = transform.position.y;
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        
        OnEnterMovingPlatform.AddListener((platform) =>
        {
            movingPlatform = platform;
        });
        OnExitMovingPlatform.AddListener((platform) =>
        {
            if (movingPlatform != null && movingPlatform.Equals(platform))
            {
                movingPlatform = null;
            }
        });
    }

    public void OnJump()
    {
        if (IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, JumpSpeed);
            OnJumped?.Invoke();
            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(AudioManager.instance.jumpClip);
            }
        }
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        
        if (input.x != 0)
        {
            FacingDirection = (int) Mathf.Sign(input.x);   
        }

        if (CanClimb && input.y != 0)
        {
            moveInput = input.y;
            if (!IsClimbing)
            {
                ladderCenter = new Vector3(currentLadder.transform.position.x, transform.position.y, transform.position.z);
                ToggleGroundCollisions(false);
                StartCoroutine(SmoothMoveToLadder(ladderCenter));
                if (AudioManager.instance)
                {
                    AudioManager.instance.PlaySound(AudioManager.instance.ladderClip);
                }
            }

            IsClimbing = true;

            rb.gravityScale = 0;
        }
        else if ((IsClimbing && !IsGrounded && CanClimb) || (IsClimbing && CanClimb && input.x == 0))
        {
            moveInput = input.y;
        }
        else
        {
            moveInput = input.x;
            if (IsClimbing)
            {
                ToggleGroundCollisions(true);
            }
            IsClimbing = false;

            rb.gravityScale = 1;
            if (AudioManager.instance && input.x != 0 && !Input.GetKeyUp(KeyCode.W) && !Input.GetKeyUp(KeyCode.S) && !Input.GetKeyDown(KeyCode.W) && !Input.GetKeyDown(KeyCode.S))
            {
                AudioManager.instance.PlaySound(AudioManager.instance.moveClip);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        foreach (var tag in DeathTags)
        {
            if (other.gameObject.CompareTag(tag))
            {
                PlayerDied();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        foreach (var tag in DeathTags)
        {
            if (other.gameObject.CompareTag(tag))
            {
                PlayerDied();
                return;
            }
        }
        if (other.gameObject.TryGetComponent<Collectible>(out var collectible))
        {
            if (other.gameObject.CompareTag("HammerObject"))
            {
                if (UsingHammer) return;
                
                StartCoroutine(UseHammer()); 
            }    
            collectible.Collect();
        }
        if (other.CompareTag("Ladder"))
        {
            CanClimb = true;
            currentLadder = other;
        }
        if (other.CompareTag("LadderTop"))
        {
            AtLadderTop = true;
        }
        if (other.CompareTag("LadderBottom"))
        {
            AtLadderBottom = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            CanClimb = false;
            currentLadder = null;
        }
        if (other.CompareTag("LadderTop"))
        {
            AtLadderTop = false;
        }
        if (other.CompareTag("LadderBottom"))
        {
            AtLadderBottom = false;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.isCompletingLevel) return;
        if (GameManager.LevelTimer <= 0)
        {
            PlayerDied();
            return;
        }

        if (mainCamera.WorldToScreenPoint(transform.position).y < 0)
        {
            PlayerDied();
            return;
        }
        
        if (IsDead) return;
        
        var colliders = new List<Collider2D>();
        rb.GetContacts(colliders);
        
        var wasGrounded = IsGrounded;
        IsGrounded = false;
        const int rays = 10;
        var inc = transform.lossyScale.x / rays;
        for (var i = 0; i < rays; i++)
        {
            var rayPos = new Vector2(transform.position.x - transform.lossyScale.x / 2 + i * inc, transform.position.y);
            IsGrounded = Physics2D.Raycast(
                rayPos,
                Vector2.down,
                transform.lossyScale.y / 2 + 0.1f,
                LayerMask.GetMask("Ground")
            ).collider != null;
            //StartCoroutine(Utilities.DrawDebugRay(rayPos, Vector2.down, transform.lossyScale.y / 2 + 0.1f));
            if (IsGrounded) break;
        }
        IsWalking = IsGrounded && Mathf.Abs(moveInput) > 0.05f;
        
        if (!IsClimbing)
        {
            transform.localScale = new Vector3(FacingDirection * baseScale.x, baseScale.y, baseScale.z);  
            
            if (isMidair)
            {
                jumpPeakY = Mathf.Max(jumpPeakY, transform.position.y);
                if (!wasGrounded && IsGrounded && Mathf.Sqrt(Mathf.Pow(jumpPeakY - transform.position.y,2)) > lethalFall)
                {
                    PlayerDied();
                    return;
                }
            }
            isMidair = !IsGrounded;
        }
        
        if (wasGrounded && IsGrounded)
        {
            jumpPeakY = transform.position.y;
        }
        
        if (IsClimbing)
        {
            var ladderTop = colliders.Find(x => x.CompareTag("LadderTop"));
            if (AtLadderTop && moveInput > 0 && ladderTop != null && ladderTop.transform.position.y <= transform.position.y - transform.lossyScale.y / 2)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else if (AtLadderBottom && moveInput < 0)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = new Vector2(0, moveInput * ClimbSpeed);
            }
        }
        else if (moveInput != 0)
        {
            rb.AddForce(new Vector2(moveInput * Speed, 0), ForceMode2D.Force);

            if (Mathf.Abs(rb.linearVelocity.x) > MaxSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * MaxSpeed, rb.linearVelocity.y);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * (1 - Friction), rb.linearVelocity.y);

            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }

            if (movingPlatform)
            {
                rb.MovePosition(rb.position + movingPlatform.DeltaP);
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
        float duration = 0.05f;
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
    }

    private IEnumerator UseHammer()
    {
        UsingHammer = true;

        const float inc = 0.1f;
        float hammerTime = 0;

        if (AudioManager.instance)
        {
            AudioManager.instance.PlayHammerMusic();
        }

        while (UsingHammer && hammerTime <= HammerDuration)
        {
            yield return new WaitForSeconds(inc);
            hammerTime += inc;
        }

        if (AudioManager.instance)
        {
            AudioManager.instance.StopHammerMusic();
        }
        UsingHammer = false;
    }

    private void PlayerDied()
    {
        if (IsDead) return;
        IsDead = true;
        rb.simulated = false;
        GetComponent<Collider2D>().enabled = false;
        OnDeath?.Invoke();
        if (AudioManager.instance)
        {
            AudioManager.instance.PlayerDied();
        }
    }

    public bool IsAboveCurrentLadder()
    {
        return currentLadder && (currentLadder.transform.Find("LadderTop")?.transform.position.y ?? transform.position.y) < transform.position.y;
    }
}
