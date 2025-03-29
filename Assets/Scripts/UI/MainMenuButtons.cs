using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    private GameObject controlsPanel;

    private void Awake()
    {
        controlsPanel = transform.Find("Controls").gameObject;
    }

    public void OnClickPlay()
    {
        GameManager.StartGame();
    }

    public void OnToggleControls(bool toggle)
    {
        controlsPanel.SetActive(toggle);
    }

    public void StartTutorial()
    {
        GameManager.NextTutorial();
    }
}
