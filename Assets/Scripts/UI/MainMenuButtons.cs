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
        AudioManager.PlaySound(Audios.MenuClick);
    }

    public void OnToggleControls(bool toggle)
    {
        controlsPanel.SetActive(toggle);
        AudioManager.PlaySound(Audios.MenuClick);
    } 
}
