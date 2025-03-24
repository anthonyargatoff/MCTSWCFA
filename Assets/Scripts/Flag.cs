using UnityEngine;
using UnityEngine.Events;

public class Flag : MonoBehaviour
{
  public UnityEvent OnFinishLevel = new UnityEvent();
  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("Player"))
    {
      OnFinishLevel?.Invoke();
    }
  }
}
