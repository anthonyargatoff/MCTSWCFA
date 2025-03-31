using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    private RectTransform bg;
    private RectTransform layout;
    private TextMeshProUGUI flavorText;
    private TextMeshProUGUI livesText;
    private RectTransform times;
    private Image character;

    private static bool _spritesLoaded = false;
    private static int[] _spriteIndices = {
        0, 1, 2, 3, 9, 18
    };
    private static Dictionary<int,Sprite> _characterSprites = new();

    private Vector2 characterPosition;
    
    private void Awake()
    {
        if (!_spritesLoaded)
        {
            var spr = Resources.LoadAll<Sprite>("Sprites/player");
        
            foreach (var idx in _spriteIndices)
            {
                _characterSprites[idx] = spr.FirstOrDefault(x => x.name.Equals("player_" + idx));
            }
            _spritesLoaded = true;
        }
        
        bg = transform.Find("BackgroundPanel").GetComponent<RectTransform>();
        layout = transform.Find("Layout").GetComponent<RectTransform>();
        
        flavorText = layout.transform.Find("FlavorText").GetComponent<TextMeshProUGUI>();

        var container = layout.transform.Find("Container");
        livesText = container.transform.Find("LivesText").GetComponent<TextMeshProUGUI>();
        times = container.transform.Find("Times").GetComponent<RectTransform>();
        character = container.transform.Find("Character").GetComponent<Image>();
        character.sprite = _characterSprites[0];
        
        characterPosition = character.rectTransform.anchoredPosition;
    }

    public IEnumerator ShowLoadingScreen(bool respawn = false)
    {
        var width = Screen.width;
        var layoutPos = new Vector2(-width, 0);
        ToggleBackground(true);
        livesText.SetText($"{GameManager.CurrentLives}");

        character.enabled = false;
        layout.DOMoveX(width/2f, 1f).SetEase(Ease.InCubic).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
        if (AudioManager.Instance)
        {
            AudioManager.StopBackgroundMusic();
            AudioManager.PlaySound(Audios.StartLevel);
        }
        if (respawn)
        {
            character.rectTransform.position += new Vector3(0, layout.sizeDelta.y, 0);
            character.enabled = true;
            character.sprite = _characterSprites[18];

            var inc = Vector2.Distance(character.rectTransform.anchoredPosition, characterPosition) / 50;
            while (character.rectTransform.anchoredPosition.y > characterPosition.y)
            {
                character.rectTransform.anchoredPosition = new Vector2(characterPosition.x, character.rectTransform.anchoredPosition.y - inc);
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }
        else
        {
            character.rectTransform.position -= new Vector3(layout.sizeDelta.x / 2, 0, 0);
            character.enabled = true;
            float delta = 0;
            var sprIdx = 1;
            var inc = Vector2.Distance(character.rectTransform.anchoredPosition, characterPosition) / 100;
            while (character.rectTransform.anchoredPosition.x < characterPosition.x)
            {
                character.sprite = _characterSprites[sprIdx];
                if (delta > 0.1f)
                {
                    sprIdx = sprIdx == 1 ? 2 : 1;
                    delta = 0;
                }
                delta += 0.01f;
                character.rectTransform.anchoredPosition = new Vector2(character.rectTransform.anchoredPosition.x + inc, characterPosition.y);
                yield return new WaitForSecondsRealtime(0.01f);
            }
        }

        character.rectTransform.anchoredPosition = characterPosition;

        character.sprite = _characterSprites[0];
        var toSpell = $"LEVEL {GameManager.CurrentLevel}";
        var current = string.Empty;

        while (!current.Equals(toSpell))
        {
            current += toSpell[current.Length];
            flavorText.SetText(current);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        character.sprite = _characterSprites[9];
        yield return new WaitForSecondsRealtime(0.5f);

        var sequence = DOTween.Sequence();
        sequence.SetEase(Ease.InOutSine);
        sequence.SetUpdate(true);

        var midpoint = Mathf.Lerp(times.position.x, livesText.transform.position.x, .5f);
        var radius = Mathf.Sqrt(Mathf.Pow(character.rectTransform.position.x - midpoint, 2));
        const int numPoints = 30;
        const float timeStep = 0.05f;
        for (var i = 0; i < numPoints; i++)
        {
            var angle = Mathf.PI - i * (Mathf.PI / numPoints);
            var pos = new Vector2(midpoint + Mathf.Cos(angle) * radius, times.position.y + Mathf.Sin(angle) * radius);
            sequence.Append(character.rectTransform.DOMove(pos, timeStep));
        }
        sequence.Play();
        yield return new WaitForSecondsRealtime(timeStep);
        character.sprite = _characterSprites[3];
        yield return new WaitForSecondsRealtime((numPoints - 1) * timeStep);

        character.sprite = _characterSprites[0];

        character.rectTransform.DOMoveX(character.rectTransform.position.x + layout.sizeDelta.x / 2, 1.5f).SetEase(Ease.Linear).SetUpdate(true);

        float dt = 0;
        var si = 1;
        while (dt < 1.5f)
        {
            character.sprite = _characterSprites[si];
            si = si == 1 ? 2 : 1;
            dt += 0.1f;
            yield return new WaitForSecondsRealtime(0.1f);
        }

        ToggleBackground(false);
        character.rectTransform.anchoredPosition = characterPosition;
        layout.anchoredPosition = layoutPos;
        flavorText.SetText("");
    }

    public void ToggleBackground(bool toggle)
    {
        bg.gameObject.SetActive(toggle);
    }
}
