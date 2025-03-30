using UnityEngine;
using UnityEngine.Events;

public class NextTutorialCheck : MonoBehaviour
{
  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("Player"))
    {
      GameManager.NextTutorial();
    }
  }
}
