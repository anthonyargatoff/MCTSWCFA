using UnityEngine;

public class VictoryButtons : MonoBehaviour
{
    public void Return()
    {
        GameManager.Instance?.ReturnToMainMenu();
        AudioManager.PlaySound(Audios.MenuClick);
    }
}
