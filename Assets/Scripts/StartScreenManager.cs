using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenManager : MonoBehaviour
{
  public void ExitApplication()
  {
#if UNITY_EDITOR
    EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }

  public void StartGame()
  {
    SceneManager.LoadScene("SampleScene");
  }
}
