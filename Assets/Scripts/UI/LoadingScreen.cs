using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    private RectTransform bg;
    private RectTransform layout;
    private TextMeshProUGUI flavorText;
    private TextMeshProUGUI livesText;
    private RectTransform times;
    private Image character;
    
    private static string[] _flavorTexts =
    {
        "HERE WE GO!",
        "LET'S DO THIS!",
        "I CAN DO THIS!",
    };
    
    private static bool _spritesLoaded = false;
    private static int[] _spriteIndices = {
        0, 1, 2, 3, 9
    };
    private static Dictionary<int,Sprite> _characterSprites = new();
    
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
    }

    public IEnumerator ShowLoadingScreen(int numLives)
    {
        ToggleBackground(true);
        livesText.SetText($"{numLives}");
        
        const float radius = 200f;
        var goalX = times.position.x - radius;
        character.rectTransform.position = new Vector2(goalX, character.rectTransform.position.y);

        layout.DOMoveX(layout.sizeDelta.x/2, 1f).SetEase(Ease.InCubic).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);
        
        character.sprite = _characterSprites[0];
        var toSpell = _flavorTexts[Random.Range(0,_flavorTexts.Length)];
        var current = string.Empty;
        
        while (!current.Equals(toSpell))
        {
            current += toSpell[current.Length];
            flavorText.SetText(current);
            yield return new WaitForSecondsRealtime(0.2f);
        }
        character.sprite = _characterSprites[9];
        yield return new WaitForSecondsRealtime(1f);
        
        const int numPoints = 30;
        const float timeStep = 0.05f;
        for (var i = 0; i < numPoints; i++)
        {
            if (i == 1)
            {
                character.sprite = _characterSprites[3];
            }
            var angle = Mathf.PI - i * (Mathf.PI / numPoints);
            var pos = new Vector2(times.position.x + Mathf.Cos(angle) * radius, times.position.y + Mathf.Sin(angle) * radius);
            character.rectTransform.DOMove(pos, timeStep).SetEase(Ease.Linear).SetUpdate(true);
            yield return new WaitForSecondsRealtime(timeStep);
        }
        character.sprite = _characterSprites[0];
        
        character.rectTransform.DOMoveX(character.rectTransform.position.x + layout.sizeDelta.x/2, 1.5f).SetEase(Ease.Linear).SetUpdate(true);

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
        layout.position = new Vector2(-layout.sizeDelta.x, layout.position.y);
        character.rectTransform.position = new Vector2(goalX, character.rectTransform.position.y);
        flavorText.SetText("");
    }

    public void ToggleBackground(bool toggle)
    {
        bg.gameObject.SetActive(toggle);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
