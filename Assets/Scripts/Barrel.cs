using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Barrel : MonoBehaviour
{
  private Rigidbody2D barrelRb;
  [SerializeField] private float barrelSpeed;
  [SerializeField] private float chanceToFallOff;
  public bool barrelMovingRight = true;
  [SerializeField] float chanceToTakeLadder;
  private bool onLadder = false;
  private List<Collider2D> platforms;

  private Rewindable rewindableScript;
  private Collider2D lastLadder = null;

  void Start()
  {
    rewindableScript = GetComponent<Rewindable>();
    barrelRb = gameObject.GetComponent<Rigidbody2D>();
    GetComponent<SpriteRenderer>().sortingOrder = 2;
  }

  void Update()
  {
    ApplyBarrelVelocity();
  }

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Barrel"))
    {
      // Only has a 50% chance to destroy itself
      // TODO: Look into a better way to ensure that we only destroy 1 of the barrels
      if (Random.Range(0, 2) == 0)
      {
        Destroy(gameObject);
      }
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    HandleBarrelRoll(collision);
    BarrelCleanUp(collision);
    HandleLadder(collision);
    HandleHammer(collision);
  }

  void OnTriggerExit2D(Collider2D collision)
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

  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Platform") && onLadder)
    {
      onLadder = false;
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
      barrelRb.linearVelocityX = barrelSpeed;
    }
    else
    {
      barrelRb.linearVelocityX = -barrelSpeed;
    }
    if (onLadder)
    {
      barrelRb.linearVelocity = new Vector2(0, -2);
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
    var hammer = collision.GetComponent<HammerPowerup>();
    if (!collision.gameObject.CompareTag("HammerHitbox") || !(hammer?.PowerupActive() ?? false)) return;
    hammer.MakeParticleEffects(transform);
    Destroy(gameObject);
    // TODO: Add to the game score
  }
}
