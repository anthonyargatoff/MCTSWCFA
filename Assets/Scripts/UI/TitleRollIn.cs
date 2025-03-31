using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TitleRollIn : MonoBehaviour
{
    private bool spritesLoaded;
    private static List<Sprite> titleSprites = new();
    private static int _numSprites;

    private Transform titleTransform;
    private RectTransform rectTransform;
    private Image mainBg;
    private RectTransform bgOverlay;
    private RectTransform i1;
    private RectTransform i2;
    private GameObject buttonCanvas;
    
    private bool titleRolledIn;
    
    private void Awake()
    {
        if (!spritesLoaded)
        {
            titleSprites.AddRange(Resources.LoadAll<Sprite>("Sprites/title"));
            foreach (var sprite in titleSprites)
            {
                var success  = int.TryParse(sprite.name["title_".Length..], out var i);
                if (success) _numSprites = Math.Max(_numSprites, i+1);
            }
            spritesLoaded = true;
        }
        titleTransform = transform.Find("TitleContainer");
        rectTransform = titleTransform.GetComponent<RectTransform>();
        mainBg = transform.Find("Background1").GetComponent<Image>();
        bgOverlay = transform.Find("Background2").GetComponent<RectTransform>();
        i1 = bgOverlay.Find("I1").GetComponent<RectTransform>();
        i2 = bgOverlay.Find("I2").GetComponent<RectTransform>();
        buttonCanvas = GameObject.Find("ButtonCanvas");
        
        StartCoroutine(RollInTitle());
    }

    private IEnumerator RollInTitle()
    {
        if (titleRolledIn) yield break;
        titleRolledIn = true;
        
        var buttonContainer = buttonCanvas?.transform.Find("ButtonContainer");
        if (buttonContainer)
        {
            buttonContainer.localScale = new Vector3(0f, 1f, 1f);
        }
        
        var sample = titleSprites[0];
        var scale = 1.5f;
        rectTransform.sizeDelta = new Vector2(sample.texture.width * scale, sample.texture.height * scale);
        var offsetY = -rectTransform.sizeDelta.y / 2 - 50f;
        rectTransform.anchoredPosition = new Vector2(0, offsetY);
        
        yield return new WaitForSecondsRealtime(1f);
        for (var i = 0; i < _numSprites; i++)
        {
            var img = CreateSpriteImage(i, scale);
            var goalY = img.rectTransform.position.y;
            img.rectTransform.position= new Vector2(img.rectTransform.position.x, img.rectTransform.position.y + rectTransform.sizeDelta.y * scale);
            img.enabled = true;
            var time = 1f + Random.value * 0.5f;
            img.rectTransform.DOMoveY(goalY, time).SetEase(Ease.OutBounce).SetUpdate(true).SetLink(img.gameObject);
        }
        
        yield return new WaitForSeconds(0.5f);
        bgOverlay.DOMoveY(Screen.height/2f, 1f);
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(PulseBeast());
        i1.DOMoveX(i1.position.x - 250f, 1f);
        i2.DOMoveX(i2.position.x + 250f, 1f);
        buttonContainer?.DOScaleX(1,1f);
    }

    private IEnumerator PulseBeast()
    {
        while (!this.IsDestroyed() && enabled)
        {
            mainBg.DOColor(new Color(1, 1, 1, mainBg.color.a > 0.25f ? 0.25f : 0.75f), 1f).SetLink(mainBg.gameObject);
            yield return new WaitForSeconds(1f);
        }
    }
    
    private Image CreateSpriteImage(int i, float scale)
    {
        var obj = new GameObject();
        obj.transform.SetParent(titleTransform);
        obj.name = titleSprites[i].name;
        
        var img = obj.AddComponent<Image>();
        var sprite = titleSprites[i];
        img.sprite = sprite;
        img.rectTransform.anchorMin = new Vector2(0, 0);
        img.rectTransform.anchorMax = new Vector2(0, 0);
        img.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * scale;
        img.rectTransform.anchoredPosition = new Vector2(sprite.rect.center.x, sprite.rect.center.y) * scale;
        img.enabled = false;
        
        return img;
    }
}
