using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
  [SerializeField] private Flag flag;
  [SerializeField] private string nextLevel;
  [SerializeField] private PlayerController player;
  private void Start()
  {
    flag.OnFinishLevel.AddListener(NextLevel);
    player.barrelCollision.AddListener(RestartLevel);
    player.fallOffScreen.AddListener(RestartLevel);
  }

  private void NextLevel()
  {
    SceneManager.LoadScene(nextLevel);
  }

  /// <summary>
  /// Trigger when player collides with barrel and dies
  /// </summary>
  private void RestartLevel()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }
}
