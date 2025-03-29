using TMPro;
using UnityEngine;

public class ScoreTextElement : MonoBehaviour
{
    private TextMeshProUGUI text;
    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    
    void Update()
    {
        text.SetText($"SCORE {GameManager.TotalScore}");
    }
}
