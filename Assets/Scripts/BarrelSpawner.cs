using System.Collections;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
  [SerializeField] Barrel barrel;
  [SerializeField] float spawnTimer;
  private IEnumerator coroutine;
  [SerializeField] private Transform barrelSpawnRef;
  [SerializeField] private float barrelThrowForceY;
  [SerializeField] private float barrelThrowForceX;
  void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }

  IEnumerator SpawnBarrels()
  {
    while (true)
    {
      Barrel newBarrel = Instantiate(barrel, barrelSpawnRef.position, Quaternion.identity);
      Rigidbody2D newBarrelRb = newBarrel.gameObject.GetComponent<Rigidbody2D>();
      newBarrelRb.AddForce(new Vector2(barrelThrowForceX, barrelThrowForceY), ForceMode2D.Impulse);
      yield return new WaitForSeconds(spawnTimer);
    }
  }
}
