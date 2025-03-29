using UnityEngine;

public class ResetTutorialLevel : MonoBehaviour
{
  void OnTriggerEnter2D(Collider2D collision)
  {
    Debug.Log("Here");
    if (collision.CompareTag("Player"))
    {
      GameManager.RestartTutorialLevel();
    }
  }
}
