using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DonkeyKong : MonoBehaviour
{
  private bool isMovingRight = true;
  [SerializeField] private Rigidbody2D donkeyKongRigidbody;
  [SerializeField] private float donkeyKongSpeed = 5f;
  [SerializeField] private FallingBarrel fallingBarrel;
  private IEnumerator coroutine;
  [SerializeField] private float lowerRandomLimit;
  [SerializeField] private float upperRandomLimit;

  public delegate void OnBarrelDelegate(float randomWaitTime);
  public event OnBarrelDelegate OnBarrel;
  
  private void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }
  private void Update()
  {
    HandleMovement();
  }

  private void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("InvisibleWall"))
    {
      isMovingRight = !isMovingRight;
    }
  }

  private void HandleMovement()
  {
    donkeyKongRigidbody.linearVelocityX = isMovingRight ? donkeyKongSpeed : -1 * donkeyKongSpeed;
  }

  private IEnumerator SpawnBarrels()
  {
    while (enabled && !this.IsDestroyed())
    {
      float randomWaitTime = Random.Range(lowerRandomLimit, upperRandomLimit);
      Instantiate(fallingBarrel, transform.position, Quaternion.identity);
      OnBarrel?.Invoke(randomWaitTime);
      yield return new WaitForSeconds(randomWaitTime);
    }
  }
}
