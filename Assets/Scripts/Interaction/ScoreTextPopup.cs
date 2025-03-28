using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreTextPopup : MonoBehaviour
{
    public IEnumerator Popup(int score = 0, float duration = 1f)
    {
        var tmp = transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        tmp.SetText($"{score}");
        yield return Debris(duration);
    }
    
    public IEnumerator Popup(string text, float duration = 1f)
    {
        var tmp = transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        tmp.SetText(text);
        yield return Debris(duration);
    }

    private IEnumerator Debris(float duration = 1f)
    {
        transform.DOMoveY(transform.position.y + 1f, duration).SetLink(gameObject).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration);
        
        Destroy(gameObject);
    }
}