using System.Collections;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
  [SerializeField] Barrel barrel;
  [SerializeField] float spawnTimer;
  private IEnumerator coroutine;
  void Start()
  {
    coroutine = SpawnBarrels();
    StartCoroutine(coroutine);
  }

  IEnumerator SpawnBarrels()
  {
    while (true)
    {
      Instantiate(barrel);
      yield return new WaitForSeconds(spawnTimer);
    }
  }
}
