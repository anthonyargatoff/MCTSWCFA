using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    private RectTransform bg;
    private Image overlay;
    private RectTransform container;
    private TextMeshProUGUI gameOverText;
    private RectTransform gameOverTextRect;
    private TextMeshProUGUI scoreText;
    private RectTransform characterContainer;

    private void Awake()
    {
        bg = transform.Find("BackgroundPanel").GetComponent<RectTransform>();
        overlay = transform.Find("OverlayPanel").GetComponent<Image>();
        container = transform.Find("Container").GetComponent<RectTransform>();
        gameOverText = container.transform.Find("GameOverText").GetComponent<TextMeshProUGUI>();
        gameOverTextRect = gameOverText.GetComponent<RectTransform>();
        scoreText = container.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        characterContainer = container.transform.Find("CharacterContainer").GetComponent<RectTransform>();
    }

    public IEnumerator ShowGameOverScreen()
    {
        var goalY = gameOverTextRect.position.y;
        gameOverTextRect.anchoredPosition = new Vector2(gameOverTextRect.anchoredPosition.x, gameOverTextRect.anchoredPosition.y + bg.sizeDelta.y / 2);
        scoreText.SetText($"SCORE {GameManager.TotalScore}");
        
        bg.gameObject.SetActive(true);
        overlay.gameObject.SetActive(true);
        overlay.color = new Color(0f,0f,0f,1f);
        container.gameObject.SetActive(true);
        
        overlay.DOColor(new Color(0f,0f,0f,0f), 1f).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
        
        gameOverTextRect.DOMoveY(goalY, 2f).SetEase(Ease.OutBounce).SetUpdate(true);
        yield return new WaitForSecondsRealtime(2f);

        var tween = characterContainer.DOScale(0.01f, 6f).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(3f);
        overlay.DOColor(new Color(0f,0f,0f,1f), 1f).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
        
        bg.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);
        container.gameObject.SetActive(false);
        tween.OnComplete(() => characterContainer.localScale = Vector3.one);
        gameOverTextRect.anchoredPosition = new Vector2(gameOverTextRect.anchoredPosition.x, goalY);
    }
}
