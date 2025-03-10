using System;
using Unity.VisualScripting;
using UnityEngine;

public class Barrel : MonoBehaviour
{
  public enum AccelerationDirection { Right, Left, None };
  private AccelerationDirection accelerationDirection;
  private Rigidbody2D barrelRb;
  [SerializeField] private float barrelSpeed;


  void Start()
  {
    barrelRb = gameObject.GetComponent<Rigidbody2D>();
  }

  void Update()
  {
    ApplyBarrelSpeed();
  }
  void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Platform"))
    {
      SetAccelerationDirection(collision);
    }
  }

  void OnCollisionExit2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Platform"))
    {
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

  private void ApplyBarrelSpeed()
  {
    switch (accelerationDirection)
    {
      case AccelerationDirection.Right:
        barrelRb.linearVelocityX =  barrelSpeed;
        break;
      case AccelerationDirection.Left:
        barrelRb.linearVelocityX = -barrelSpeed;
        break;
    }
  }
}
