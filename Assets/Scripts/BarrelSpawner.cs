using System.Collections;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
  public enum ThrowDirection { Left, Right };
  [SerializeField] private ThrowDirection throwDirection;
  [SerializeField] Barrel barrel;
  [SerializeField] float spawnTimer;
  private IEnumerator coroutine;
  [SerializeField] private Transform barrelSpawnRef;
  [SerializeField] private float barrelThrowForceY;
  [SerializeField] private float barrelThrowForceX;
  [SerializeField] private bool randomSpawnTime;
  [SerializeField] private float lowerRandomLimit;
  [SerializeField] private float upperRandomLimit;
  
  public delegate void OnBarrelDelegate(ThrowDirection direction, float timeUntil);
  public event OnBarrelDelegate OnBarrel;
  
  void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }

  IEnumerator SpawnBarrels()
  {
    while (true)
    {
      if (randomSpawnTime)
      {
        float randomWaitTime = Random.Range(lowerRandomLimit, upperRandomLimit);
        OnBarrel?.Invoke(throwDirection,randomWaitTime);
        yield return new WaitForSeconds(randomWaitTime);
      }
      
      Barrel newBarrel = Instantiate(barrel, barrelSpawnRef.position, Quaternion.identity);
      Rigidbody2D newBarrelRb = newBarrel.gameObject.GetComponent<Rigidbody2D>();
      if (throwDirection == ThrowDirection.Right)
      {
        newBarrel.barrelMovingRight = true;
        newBarrelRb.AddForce(new Vector2(barrelThrowForceX, barrelThrowForceY), ForceMode2D.Impulse);
      }
      else
      {
        newBarrel.barrelMovingRight = false;
        newBarrelRb.AddForce(new Vector2(-1 * barrelThrowForceX, barrelThrowForceY), ForceMode2D.Impulse);
      }
      if (!randomSpawnTime)
      {
        OnBarrel?.Invoke(throwDirection,spawnTimer);
        yield return new WaitForSeconds(spawnTimer);
      }
    }
  }
}
