using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Barrel : MonoBehaviour
{
  private static bool particlesLoaded;
  private static GameObject particleEffects;
  private static GameObject specialParticleEffects;
  private static Sprite specialSprite;

  private Rigidbody2D barrelRb;
  [SerializeField] private float barrelSpeed;
  [SerializeField] private float chanceToFallOff;
  public bool barrelMovingRight = true;
  [SerializeField] float chanceToTakeLadder;
  private bool onLadder = false;
  private List<Collider2D> platforms;

  private Rewindable rewindableScript;
  private Collider2D lastLadder = null;

  private bool awardedPoints;
  private bool isQuitting;
  private bool isSpecial;

  private Vector2 prevPosition;
  private const int CheckPositionFrames = 60;
  private int lastPositionCheck;

  private void Awake()
  {
    if (Random.Range(0,50) == 0) isSpecial = true;
    if (!particlesLoaded)
    {
      particleEffects = Resources.Load<GameObject>("ParticleEffects/BarrelParticles");
      specialParticleEffects = Resources.Load<GameObject>("ParticleEffects/BarrelParticlesSpecial");
      var sprites = Resources.LoadAll<Sprite>("Sprites/barrel");
      if (sprites.Length > 0)
      {
        specialSprite = sprites.Length >= 8 ? sprites[7] : sprites[0];
      }
      particlesLoaded = true;
    }
    Application.quitting += () => isQuitting = true;

    prevPosition = transform.position;
    if (isSpecial)
    {
      var sr = GetComponent<SpriteRenderer>();
      sr.sprite = specialSprite;
    }
  }
  
  private void Start()
  {
    rewindableScript = GetComponent<Rewindable>();
    barrelRb = gameObject.GetComponent<Rigidbody2D>();
    GetComponent<SpriteRenderer>().sortingOrder = 2;
  }

  void Update()
  {
    CheckPlayerJumpOver();
    ApplyBarrelVelocity();
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    HandleBarrelCollision(collision.gameObject);

    if (collision.gameObject.CompareTag("Platform") && onLadder)
    {
      onLadder = false;
    }
  }

  private void OnDestroy()
  {
    if (!isQuitting && SceneManager.GetActiveScene().isLoaded)
    {
      Instantiate(isSpecial? specialParticleEffects : particleEffects, transform.position, Quaternion.identity); 
    }
  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    HandleBarrelCollision(collision.gameObject);
    HandleBarrelRoll(collision);
    BarrelCleanUp(collision);
    HandleLadder(collision);
    HandleHammer(collision);
  }

  private void OnTriggerExit2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("BarrelUseLadder"))
    {
      barrelMovingRight = onLadder || rewindableScript.isRewinding ? !barrelMovingRight : barrelMovingRight;
      if (onLadder)
      {
        ToggleGroundCollisions(true);
      }
    }
  }

  private void HandleBarrelCollision(GameObject obj)
  {
    if (obj.CompareTag("Barrel"))
    {
      if (rewindableScript.isRewinding)
      {
        GameManager.IncreaseScore((int) (ScoreEvent.BarrelRewindDestroy * GetModifier()), transform);
      }
      Destroy(gameObject);
    }
  }
  
  /// <summary>
  /// Determine if the barrel will fall off, given a chance modifier
  /// </summary>
  /// <param name="collision"></param>
  private void HandleBarrelRoll(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("PlatformBarrelWall") && !rewindableScript.isRewinding)
    {
      int randomNum = Random.Range(0, 100);
      if (randomNum > (chanceToFallOff * 100))
      {
        barrelMovingRight = !barrelMovingRight;
      }
    }
  }

  /// <summary>
  /// Apply a linear velocity to the barrels
  /// </summary>
  private void ApplyBarrelVelocity()
  {
    if (rewindableScript.isRewinding) return;
    
    if (barrelMovingRight)
    {
      barrelRb.linearVelocityX = barrelSpeed * (isSpecial ? 1.5f : 1);
    }
    else
    {
      barrelRb.linearVelocityX = -barrelSpeed * (isSpecial ? 1.5f : 1);
    }
    if (onLadder)
    {
      barrelRb.linearVelocity = new Vector2(0, -2);
    }

    if (Time.timeScale > 0)
    {
      // Prevent barrel getting stuck
      if (lastPositionCheck > GameManager.GetScaledFrameCount(CheckPositionFrames))
      {
        if (Vector2.Distance(prevPosition, transform.position) < 0.01f)
        {
          barrelMovingRight = !barrelMovingRight;
        }

        prevPosition = transform.position;
        lastPositionCheck = 0;
      }

      lastPositionCheck++;
    }
  }

  /// <summary>
  /// Destroys the barrels when it collides with object (to cleanup fallen barrels)
  /// </summary>
  /// <param name="collision"></param>
  private void BarrelCleanUp(Collider2D collision)
  {
    if (collision.gameObject.name == "BarrelCleanUp")
    {
      Destroy(gameObject);
    }
  }

  private void HandleLadder(Collider2D collision)
  {
    if (!collision.gameObject.CompareTag("BarrelUseLadder")) return;
    if (rewindableScript.isRewinding)
    {
      onLadder = transform.position.y < (collision.transform.position.y + collision.transform.lossyScale.y / 2);
      return;
    }

    // Prevent barrel from reusing short ladders; loop
    if (collision.Equals(lastLadder)) return;
    lastLadder = collision;

    var randomNum = Random.Range(0, 100);
    if (randomNum < chanceToTakeLadder * 100) return;
    var targetPosition = collision.bounds.center;
    StartCoroutine(SmoothMoveToLadder(targetPosition));
  }


  private void ToggleGroundCollisions(bool toggle)
  {
    Collider2D barrelCollider = GetComponent<Collider2D>();
    if (barrelCollider == null) return;

    var objs = GameObject.FindGameObjectsWithTag("Platform");
    foreach (var obj in objs)
    {
      Collider2D platformCollider = obj.GetComponent<Collider2D>();
      if (platformCollider != null)
      {
        Physics2D.IgnoreCollision(barrelCollider, platformCollider, !toggle);
      }
    }
  }

  private IEnumerator SmoothMoveToLadder(Vector3 targetPosition)
  {
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
    onLadder = true;
    ToggleGroundCollisions(false);
  }

  private void HandleHammer(Collider2D collision)
  {
    var player = collision.transform.parent?.GetComponent<PlayerController>();
    if (!collision.gameObject.CompareTag("HammerHitbox") || !player || !player.UsingHammer) return;
    Destroy(gameObject);
    GameManager.IncreaseScore((int) (ScoreEvent.BarrelHammerDestroy * GetModifier()), transform);
  }

  private void CheckPlayerJumpOver()
  {
    if (awardedPoints) return;
    
    var hit = Physics2D.Raycast(transform.position, Vector2.up, 5f, LayerMask.GetMask("Player","Ground"));
    if (hit && hit.transform.gameObject.CompareTag("Player") && Mathf.Abs(hit.transform.position.x - transform.position.x) < 0.1f)
    {
      awardedPoints = true;
      GameManager.IncreaseScore((int) (ScoreEvent.BarrelJump * GetModifier()), transform);
    }
  }
  
  private float GetModifier()
  {
    return isSpecial ? 1.5f : 1;
  }
}
