using Unity.VisualScripting;
using UnityEngine;

public class EndLevel : MonoBehaviour
{
    private bool collected = false;
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player") && !collected)
        {
            collected = true;
            GameManager.CompleteLevel();
            Destroy(gameObject);
            
        }
    }
}
