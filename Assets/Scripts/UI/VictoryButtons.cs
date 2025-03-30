using UnityEngine;

public class VictoryButtons : MonoBehaviour
{
    public void Return()
    {
        GameManager.Instance?.ReturnToMainMenu();
        if (AudioManager.instance)
        {
            AudioManager.instance.PlaySound(AudioManager.instance.menuClickClip);
        }
    }
}
