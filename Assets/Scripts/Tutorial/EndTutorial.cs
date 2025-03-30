using TMPro;
using UnityEngine;

public class EndTutorial : MonoBehaviour
{
  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.CompareTag("Player"))
    {
      var text = GameObject.Find("EndTutorial").GetComponent<TextMeshProUGUI>();
      text?.SetText("Congratulations, you've completed the tutorial!");
      GameManager.NextTutorial();
    }
  }
}
