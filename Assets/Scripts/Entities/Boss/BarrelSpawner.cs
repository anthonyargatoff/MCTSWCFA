using System.Collections;
using Unity.VisualScripting;
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
  [SerializeField] private float barrelSize = 1f;
  
  public delegate void OnBarrelDelegate(ThrowDirection direction, float timeUntil);
  public event OnBarrelDelegate OnBarrel;
  
  private void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }

  private IEnumerator SpawnBarrels()
  {
    while (enabled && !this.IsDestroyed())
    {
      if (randomSpawnTime)
      {
        float randomWaitTime = Random.Range(lowerRandomLimit, upperRandomLimit);
        OnBarrel?.Invoke(throwDirection,randomWaitTime);
        yield return new WaitForSeconds(randomWaitTime);
      }
      
      barrel.transform.localScale = new Vector3(barrelSize, barrelSize, barrelSize);
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
