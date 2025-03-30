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
        if (AudioManager.instance)
        {
            AudioManager.instance.PlaySound(AudioManager.instance.menuClickClip);
        }
    }

    public void OnToggleControls(bool toggle)
    {
        controlsPanel.SetActive(toggle);
        if (AudioManager.instance)
        {
            AudioManager.instance.PlaySound(AudioManager.instance.menuClickClip);
        }
    } 
}
