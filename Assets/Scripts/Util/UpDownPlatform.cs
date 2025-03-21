using UnityEngine;

public class UpDownPlatform : MonoBehaviour
{
  [SerializeField] private float platformSpeed;
  private Rigidbody2D platformRigidBody;
  private bool movingUp = true;

  void Start()
  {
    platformRigidBody = GetComponent<Rigidbody2D>();
  }

  // Update is called once per frame
  void Update()
  {
    MovePlatform();
  }

  private void MovePlatform()
  {
    if (movingUp)
    {
      platformRigidBody.linearVelocityY = platformSpeed;
    }
    else 
    {
      platformRigidBody.linearVelocityY = -platformSpeed;
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("UpDownPlatformTrigger"))
    {
      movingUp = !movingUp;
    }
  }
}
