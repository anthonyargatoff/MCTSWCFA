using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpriteControllerMonoBehaviour : MonoBehaviour
{
    private static Dictionary<int, string> _indexToSprite;
    private static Dictionary<string, Sprite> _spriteDictionary;
    
    protected SpriteRenderer Sprite;
    protected string CurrentSprite = string.Empty;

    protected void Awake()
    {
        Initialize();
        TryGetComponent(out Sprite);
    }

    protected abstract void Initialize();
    
    protected static void InitializeSpriteDictionary(string spriteName, Dictionary<int,string> indexMap, Dictionary<string,Sprite> nameMap)
    {
        var sprites = Resources.LoadAll<Sprite>($"Sprites/{spriteName}");
        foreach (var s in sprites)
        {
            try
            {
                var idx = int.Parse(s.name[$"{spriteName}_".Length..]);
                indexMap.TryGetValue(idx, out var lookupName);
                if (lookupName != null)
                {
                    nameMap.Add(lookupName, s);
                }
            }
            catch (Exception)
            {
                Debug.LogWarning($"Could not import sprite {s.name}");
            }
        }
    }

    protected abstract void HandleSpriteSwap();
    
    protected abstract void SwapSprite(string newSpriteName);
    
    protected void SwapSprite(string newSpriteName, Dictionary<string, Sprite> nameMap)
    {
        nameMap.TryGetValue(newSpriteName, out var newSprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Sprite {newSpriteName} could not be found!");
            return;
        }
        Sprite.sprite = newSprite;
    }
}