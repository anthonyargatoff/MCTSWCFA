using System.Collections;
using UnityEngine;

public class DonkeyKong : MonoBehaviour
{
  private bool isMovingRight = true;
  [SerializeField] private Rigidbody2D donkeyKongRigidbody;
  [SerializeField] private float donkeyKongSpeed = 5f;
  [SerializeField] private FallingBarrel fallingBarrel;
  [SerializeField] private float spawnTimer;
  private IEnumerator coroutine;

  void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }
  void Update()
  {
    HandleMovement();
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("InvisibleWall"))
    {
      isMovingRight = !isMovingRight;
    }
  }

  void HandleMovement()
  {
    donkeyKongRigidbody.linearVelocityX = isMovingRight ? donkeyKongSpeed : -1 * donkeyKongSpeed;
  }

  IEnumerator SpawnBarrels()
  {
    while (true)
    {
      Debug.Log("Spawn");
      Instantiate(fallingBarrel, transform.position, Quaternion.identity);
      yield return new WaitForSeconds(spawnTimer);
    }
  }
}
