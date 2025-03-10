using UnityEngine;

public class Barrel : MonoBehaviour
{
  private Rigidbody2D barrelRb;
  [SerializeField] private float barrelSpeed;
  [SerializeField] private float chanceToFallOff;
  private bool barrelMovingRight = true;

  void Start()
  {
    barrelRb = gameObject.GetComponent<Rigidbody2D>();
  }

  void Update()
  {
    ApplyBarrelVelocity();
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    HandleBarrelRoll(collision);
    BarrelCleanUp(collision);
  }

  /// <summary>
  /// Determine if the barrel will fall off, given a chance modifier
  /// </summary>
  /// <param name="collision"></param>
  private void HandleBarrelRoll(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("PlatformBarrelWall"))
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
    if (barrelMovingRight)
    {
      barrelRb.linearVelocityX = barrelSpeed;
    }
    else
    {
      barrelRb.linearVelocityX = -barrelSpeed;
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

}
