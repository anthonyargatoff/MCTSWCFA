using UnityEngine;

public class FallingBarrel : MonoBehaviour
{
  private Rigidbody2D barrelRigidBody;
  [SerializeField] float barrelFallSpeed;
  [SerializeField] Barrel rollingBarrel;
  void Start()
  {
    barrelRigidBody = GetComponent<Rigidbody2D>();
    Physics2D.IgnoreCollision(rollingBarrel.GetComponent<Collider2D>(), GetComponent<Collider2D>());
  }

  // Update is called once per frame
  void Update()
  {
    barrelRigidBody.linearVelocityY = -barrelFallSpeed;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.name == "BarrelCleanUp")
    {
      Destroy(gameObject);
    }
  }
}
