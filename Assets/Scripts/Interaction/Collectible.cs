using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private int scoreGain = 0;
    [SerializeField] private int timeGain = 0;
    
    public void Collect()
    {
        if (scoreGain > 0) GameManager.IncreaseScore(scoreGain, transform);
        if (timeGain > 0) GameManager.IncreaseTimer(timeGain, transform);
        Destroy(gameObject);
    }
}
