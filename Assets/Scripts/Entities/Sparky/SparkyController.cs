
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class SparkyController: SpriteControllerMonoBehaviour
{
    private static GameObject particleEffects;
    private static GameObject specialParticleEffects;
    
    private static bool _spritesInitialized;
    protected static readonly Dictionary<int, string> IndexToSprite = new()
    {
        [0] = "sparky0",
        [1] = "sparky1",
        [2] = "sparky2",
        [3] = "sparky3",
        [4] = "sparky4",
        [5] = "sparky5",
        [6] = "sparky6",
        [7] = "s_sparky0",
        [8] = "s_sparky1",
        [9] = "s_sparky2",
        [10] = "s_sparky3",
        [11] = "s_sparky4",
        [12] = "s_sparky5",
        [13] = "s_sparky6",
    };
    protected static readonly Dictionary<string,Sprite> SpriteDictionary = new();

    public bool isBeingSpawned;
    private bool isSpecial;
    private Rigidbody2D rb;

    private const int maxFrame = 7;
    private int frame = 0;
    
    private const int framesBetweenUpdate = 10;
    private int framesSinceLastUpdate = 0;
    
    private Vector2 goalPosition = Vector2.zero;
    private bool climbingLadder;
    private bool jumping;
    private Collider2D currentGround;
    private bool wasGrounded;
    private Vector3 lastPosition = Vector3.zero;
    
    private const int framesBetweenPositionCheck = 60;
    private int framesSinceLastPositionCheck;

    private const int framesBetweenLook = 120;
    private int framesSinceLastLook;

    private const float ladderCooldown = 3f;
    private float ladderCooldownTimer;

    private bool forceLeft;
    private bool forceRight;

    private bool isQuitting;
    
    private new void Awake()
    {
        base.Awake();
        Application.quitting += () => isQuitting = true;
        isSpecial = Random.Range(1, 4) == 0;
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        particleEffects = Resources.Load<GameObject>("ParticleEffects/SparkyParticles");
        specialParticleEffects = Resources.Load<GameObject>("ParticleEffects/SparkyParticlesSpecial");
        InitializeSpriteDictionary("sparky", IndexToSprite, SpriteDictionary);
        _spritesInitialized = true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0) return;
        HandleSpriteSwap();
        if (framesSinceLastUpdate > GameManager.GetScaledFrameCount(framesBetweenUpdate))
        {
            frame++;
            if (frame >= maxFrame) frame = 0;
            framesSinceLastUpdate = 0;
        }
        framesSinceLastUpdate++;

        if (framesSinceLastPositionCheck > GameManager.GetScaledFrameCount(framesBetweenPositionCheck))
        {
            if (Vector3.Distance(lastPosition,transform.position) < 0.05f) goalPosition = Vector2.zero;
            lastPosition = transform.position;
            framesSinceLastPositionCheck = 0;
        }
        framesSinceLastPositionCheck++;

        if (framesSinceLastLook > GameManager.GetScaledFrameCount(framesBetweenLook))
        {
            var eyeLine = CheckEyeLine(Math.Sign(transform.localScale.x) == -1);
            if (eyeLine.Item2.collider != null)
            {
                StartCoroutine(Jump(eyeLine.Item2.point));
            }
            framesSinceLastLook = 0;
        }
        framesSinceLastLook++;
        
        ladderCooldownTimer -= Time.deltaTime;
        if (ladderCooldownTimer <= 0) ladderCooldownTimer = 0;
    }
    
    private void FixedUpdate()
    {
        if (isBeingSpawned)
        {
            goalPosition = Vector2.zero;
            return;
        }
        currentGround = Physics2D.Raycast(new Vector2(transform.position.x,transform.position.y), Vector2.down, 1f, LayerMask.GetMask("Ground")).collider;
        
        if (climbingLadder || jumping)
        {
            rb.linearVelocityX = 0;
            return;
        }
        
        if (goalPosition.Equals(Vector2.zero))
        {
            FindPath();
            return;
        }
        
        GetToGoalPosition();
        forceLeft = false;
        forceRight = false;
    }

    protected override void HandleSpriteSwap()
    {
        var newSprite = $"{(isSpecial ? "s_" : "")}sparky{frame}";
        if (!CurrentSprite.Equals(newSprite))
        {
            CurrentSprite = newSprite;
            SwapSprite(CurrentSprite);
        }
    }
    
    protected override void SwapSprite(string spriteName)
    {
        base.SwapSprite(spriteName, SpriteDictionary);
    }
    
    private void OnDestroy()
    {
        if (!isQuitting && SceneManager.GetActiveScene().isLoaded)
        {
            Instantiate(isSpecial ? specialParticleEffects : particleEffects, transform.position, Quaternion.identity); 
        }
    }
    
    private void GetToGoalPosition()
    {
        var xDis = Mathf.Abs(transform.position.x - goalPosition.x);
        var xDirection = -Mathf.Sign(transform.position.x - goalPosition.x);
        
        var leaveRbAlone = (xDirection < 0 && rb.linearVelocityX < xDirection) || (xDirection > 0 && rb.linearVelocityX > xDirection);
        rb.linearVelocityX = leaveRbAlone ? rb.linearVelocityX : xDirection;

        var obstructed = false;
        for (var i = 0; i < 10; i++)
        {
            var rayOrigin = transform.position + new Vector3(0, transform.lossyScale.y / 2 - i * 0.1f, 0);
            var rayDir = xDirection * Vector2.right;
            var hit = Physics2D.Raycast(rayOrigin,rayDir, 0.5f, LayerMask.GetMask("Ground"));
            if (!hit.collider) continue;
            obstructed = true;
            break;
        }
        if (obstructed)
        {
            rb.AddForceY(1f,ForceMode2D.Impulse);
        }
        
        transform.localScale = new Vector3(xDirection, 1, 1);
        if (xDis < 0.1f) goalPosition = Vector2.zero;
        wasGrounded = currentGround;
    }

    private IEnumerator Jump(Vector3 endPos)
    {
        if (jumping) yield break;
        jumping = true;

        var controlPoint = Vector3.Lerp(transform.position, endPos, 0.5f);
        controlPoint.y = Mathf.Max(transform.position.y, endPos.y) + 2f;
        var bezier = QuadraticBezier.GenerateBezierCurvePath(transform.position, endPos, controlPoint, 10);
        var seq = DOTween.Sequence();
        foreach (var v in bezier)
        {
            seq.Append(transform.DOMove(v, 0.1f).SetEase(Ease.Linear));
        }
        seq.SetLink(gameObject);
        seq.Play();
        yield return new WaitForSeconds(seq.Duration());
        jumping = false;
    }
    
    private void FindPath()
    {
        if (!currentGround)
        {
            goalPosition = transform.position + new Vector3(Random.Range(0,2) == 0 ? 1 : 0 * Random.Range(2f,5f), 0, 0);
            return;
        }
        
        var bounds = currentGround.bounds;
        var disFromLeft = Mathf.Abs(transform.position.x - bounds.min.x);
        var disFromRight = Mathf.Abs(transform.position.x - bounds.max.x);
        var disFromCenter = Mathf.Abs(transform.position.x - bounds.center.x);

        var leftEnd = new Vector2(bounds.min.x, bounds.max.y);
        var rightEnd = new Vector2(bounds.max.x, bounds.max.y);
            
        if (disFromLeft < 0.1f || disFromRight < 0.1f)
        {
            var leftHand = (forceLeft && !forceRight) || disFromLeft < disFromRight;

            var eyeLine = CheckEyeLine(leftHand);
            
            var nextPlatform = eyeLine.Item1;
            var sawPlayer = eyeLine.Item2;
            
            if (sawPlayer.collider)
            {
                StartCoroutine(Jump(sawPlayer.point));
                return;
            }

            if (!nextPlatform.collider)
            {
                goalPosition = leftHand ? rightEnd : leftEnd;
                return;
            }

            StartCoroutine(Jump(Vector2.Lerp(nextPlatform.point,new Vector2(nextPlatform.collider.bounds.center.x,nextPlatform.point.y),0.3f)));
            goalPosition = nextPlatform.point.x < nextPlatform.collider.bounds.center.x
                ? new Vector2(nextPlatform.collider.bounds.max.x, nextPlatform.collider.bounds.max.y)
                : new Vector2(nextPlatform.collider.bounds.min.x, nextPlatform.collider.bounds.max.y);
            return;
        }
            
        if (forceLeft || (disFromLeft < disFromRight && disFromLeft < disFromCenter))
        {
            goalPosition = rightEnd;
            return;
        } 
        if (forceRight || (disFromRight < disFromLeft && disFromRight < disFromCenter))
        {
            goalPosition = leftEnd;
            return;
        }
        goalPosition = Random.Range(0,10) < 5 
            ? leftEnd
            : rightEnd;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("PlatformBarrelWall"))
        {
            if (goalPosition.x > other.bounds.min.x)
                forceLeft = true;
            else if (goalPosition.x < other.bounds.max.x)
                forceRight = true;
            goalPosition = Vector2.zero;
        }
        if (other.gameObject.CompareTag("HammerHitbox"))
        {
            var player = other.transform.parent?.GetComponent<PlayerController>();
            if (!player || !player.UsingHammer) return;
            Destroy(gameObject);
            AudioManager.PlaySound(Audios.Destroy);
            GameManager.IncreaseScore((int) (ScoreEvent.BarrelHammerDestroy * GetModifier()), transform);
        }
        if (other.gameObject.name == "BarrelCleanUp")
        {
            Destroy(gameObject);
        } 
        else if (other.gameObject.CompareTag("LadderTop"))
        {
            if (Random.Range(0, 3) == 0)
            {
                StartCoroutine(ClimbLadder(other.transform.parent));
            }    
        } 
        else if (other.gameObject.CompareTag("LadderBottom"))
        {
            if (Random.Range(0, 3) == 0)
            {
                StartCoroutine(ClimbLadder(other.transform.parent,false));
            }    
        }
    }

    private IEnumerator ClimbLadder(Transform ladder, bool down = true)
    {
        if (ladderCooldownTimer > 0) yield break;
        if (climbingLadder) yield break;
        var ladderTop = ladder.Find("LadderTop");
        var ladderBottom = ladder.Find("LadderBottom");
        if (!ladderTop || !ladderBottom) yield break;
        
        ladderCooldownTimer = ladderCooldown;
        climbingLadder = true; 
        var height = Mathf.Abs(ladderTop.position.y - ladderBottom.position.y);
        var duration = height / 2f;
        
        transform.DOMoveX(ladderTop.position.x, 0.1f).SetEase(Ease.Linear).SetLink(gameObject);
        yield return new WaitForSeconds(0.1f);
        transform.DOMoveY(down ? ladderBottom.position.y : ladderTop.position.y, duration).SetEase(Ease.Linear).SetLink(gameObject);
        yield return new WaitForSeconds(duration);
        
        climbingLadder = false;
    }

    private Tuple<RaycastHit2D,RaycastHit2D> CheckEyeLine(bool leftHand)
    {
        const float range = 4f;
        const float angle = 0.45f;
        const float yRange = 3f;
        
        var y = -angle;
        
        var candidates = new List<RaycastHit2D>();
        var nextPlatform = new RaycastHit2D();
        var sawPlayer = new RaycastHit2D();
        
        while (y < angle)
        {
            var rayDir = Vector2.left * (leftHand ? 1 : -1) + new Vector2(0, y);
            var check = Physics2D.Raycast(transform.position, rayDir.normalized, range, LayerMask.GetMask("Ground","Player"));
                
            if (check.collider && !check.collider.Equals(currentGround) && (check.collider.attachedRigidbody?.bodyType ?? RigidbodyType2D.Kinematic) != RigidbodyType2D.Dynamic) {
                candidates.Add(check);
                //StartCoroutine(DrawDebugRay(transform.position, rayDir.normalized, range));   
            } else if (check.collider && check.collider.gameObject.CompareTag("Player"))
            {
                sawPlayer = check;
                break;
            }
            y += 0.01f;
        }
        
        var closestDistance = float.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.distance < closestDistance && Mathf.Abs(transform.position.y - candidate.transform.position.y) < yRange && !candidate.transform.GetComponent<MovingPlatform>())
            {
                closestDistance = candidate.distance;
                nextPlatform = candidate;
            }
        }

        return new Tuple<RaycastHit2D, RaycastHit2D>(nextPlatform, sawPlayer);
    }
    
    private float GetModifier()
    {
        return isSpecial ? 1.5f : 1;
    }
}