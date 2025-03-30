using UnityEngine;

public class EndTutorial : MonoBehaviour
{
  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("Player"))
    {
      StartCoroutine(GameManager.EndTutorial());
    }
  }
}
