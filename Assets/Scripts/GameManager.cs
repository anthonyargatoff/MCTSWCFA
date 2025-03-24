using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
  [SerializeField] private Flag flag;
  [SerializeField] private string nextLevel;
  private void Start()
  {
    flag.OnFinishLevel.AddListener(NextLevel);
  }

  private void NextLevel()
  {
    SceneManager.LoadScene(nextLevel);
  }
}
