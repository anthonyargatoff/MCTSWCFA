using UnityEngine;

public class DonkeyKong : MonoBehaviour
{
  private bool isMovingRight = true;
  [SerializeField] private Rigidbody2D donkeyKongRigidbody;
  [SerializeField] private float donkeyKongSpeed = 2f;

  // Update is called once per frame
  void Update()
  {
    donkeyKongRigidbody.linearVelocityX = isMovingRight ? donkeyKongSpeed : -1 * donkeyKongSpeed;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("InvisibleWall"))
    {
      isMovingRight = !isMovingRight;
    }
  }
}
