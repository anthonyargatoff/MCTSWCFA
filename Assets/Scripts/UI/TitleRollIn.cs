using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TitleRollIn : MonoBehaviour
{
    private bool spritesLoaded;
    private static List<Sprite> titleSprites = new();
    private static int numSprites;
    private RectTransform rectTransform;
    
    private bool titleRolledIn;
    
    private void Awake()
    {
        if (!spritesLoaded)
        {
            titleSprites.AddRange(Resources.LoadAll<Sprite>("Sprites/title"));
            foreach (var sprite in titleSprites)
            {
                var success  = int.TryParse(sprite.name["title_".Length..], out var i);
                if (success) numSprites = Math.Max(numSprites, i);
            }
            spritesLoaded = true;
        }
        rectTransform = GetComponent<RectTransform>();
        
        StartCoroutine(RollInTitle());
    }

    private IEnumerator RollInTitle()
    {
        if (titleRolledIn) yield break;
        titleRolledIn = true;
        
        var sample = titleSprites[0];
        var ppuMod = 2f;
        var ppu = sample.pixelsPerUnit / ppuMod;
        rectTransform.sizeDelta = new Vector2(sample.texture.width * ppuMod, sample.texture.height * ppuMod);
        var offsetY = -rectTransform.sizeDelta.y / 2;
        rectTransform.anchoredPosition = new Vector2(0, offsetY);
        
        var offset = new Vector2(sample.texture.width / 2f / ppu, (sample.texture.height + offsetY) / 2f / ppu);
        yield return new WaitForSecondsRealtime(1f);
        for (var i = 0; i < numSprites; i++)
        {
            var img = CreateSpriteImage(i, offset, ppu);
            var goalY = img.rectTransform.position.y;
            img.rectTransform.position= new Vector2(img.rectTransform.position.x, img.rectTransform.position.y + rectTransform.sizeDelta.y / ppu);
            img.enabled = true;
            img.rectTransform.DOMoveY(goalY, 1f + Random.value * 0.5f).SetEase(Ease.OutBounce).SetUpdate(true).SetLink(img.gameObject);
        }
    }

    private Image CreateSpriteImage(int i, Vector2 offset, float ppu)
    {
        var obj = new GameObject();
        obj.transform.SetParent(transform);
        obj.name = titleSprites[i].name;
        
        var img = obj.AddComponent<Image>();
        var sprite = titleSprites[i];
        img.sprite = sprite;
        img.rectTransform.anchorMin = new Vector2(0, 1);
        img.rectTransform.anchorMax = new Vector2(0, 1);
        img.rectTransform.sizeDelta = new Vector2(sprite.rect.width / ppu, sprite.rect.height / ppu);
        img.rectTransform.position = new Vector2(sprite.rect.center.x / ppu - offset.x, sprite.rect.center.y / ppu - offset.y);
        img.enabled = false;
        
        return img;
    }
}
