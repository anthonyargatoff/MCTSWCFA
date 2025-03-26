using Unity.VisualScripting;
using UnityEngine;

public class Flag : MonoBehaviour
{
    private bool collected = false;
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && !collected)
        {
            collected = true;
            GameManager.CompleteLevel();
            Destroy(gameObject);
        }
    }
}
