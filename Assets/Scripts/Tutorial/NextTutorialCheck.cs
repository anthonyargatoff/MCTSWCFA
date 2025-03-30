using UnityEngine;
using UnityEngine.Events;

public class NextTutorialCheck : MonoBehaviour
{
  private bool triggered;
  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("Player") && !triggered)
    {
      triggered = true;
      GameManager.NextTutorial();
    }
  }
}
