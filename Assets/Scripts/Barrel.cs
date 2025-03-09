using System;
using Unity.VisualScripting;
using UnityEngine;

public class Barrel : MonoBehaviour
{
  public enum AccelerationDirection { Right, Left, None };
  public AccelerationDirection accelerationDirection;
  private Rigidbody2D barrelRb;
  [SerializeField] private float barrelSpeed;


  void Start()
  {
    barrelRb = gameObject.GetComponent<Rigidbody2D>();
  }

  void Update()
  {
    ApplyForceToBarrel();
  }
  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Platform"))
    {
      Debug.Log("Detected Collision with platform");
      SetAccelerationDirection(collision);
    }
  }

  void OnCollisionExit2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Platform"))
    {
      Debug.Log("Barrel leaving platform");
      accelerationDirection = AccelerationDirection.None;
    }

  }

  private void SetAccelerationDirection(Collision2D collision)
  {
    Platform platform = collision.gameObject.GetComponent<Platform>();

    if (platform.accelerationDirection == Platform.AccelerationDirection.Left)
    {
      accelerationDirection = AccelerationDirection.Left;
    }
    else if (platform.accelerationDirection == Platform.AccelerationDirection.Right)
    {
      accelerationDirection = AccelerationDirection.Right;
    }
  }

  private void ApplyForceToBarrel()
  {
    switch (accelerationDirection)
    {
      case AccelerationDirection.Right:
        barrelRb.AddForce(new Vector2(barrelSpeed, 0));
        break;
      case AccelerationDirection.Left:
        barrelRb.AddForce(new Vector2(-1 * barrelSpeed, 0));
        break;
      case AccelerationDirection.None:
        break;
    }
  }
}
