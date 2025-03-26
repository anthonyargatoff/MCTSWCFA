using System;
using System.Collections.Generic;
using UnityEngine;

public class FallingBarrelSpriteController: MonoBehaviour
{
    private static bool spritesInitialized = false;
    private static Dictionary<int, string> indexToSprite = new()
    {
        [1] = "barrel0",
        [2] = "barrel1",
        [3] = "barrel2",
    };
    private static Dictionary<string,Sprite> spriteDictionary = new();

    private int barrelFrame;
    private const int FramesBetweenBarrelUpdate = 30;
    private int framesSinceLastBarrelUpdate = 0;
    
    private SpriteRenderer sprite;
    private string currentSprite = string.Empty;
    
    private void Awake()
    {
        if (!spritesInitialized)
        {
            var sprites = Resources.LoadAll<Sprite>("Sprites/barrel");
            foreach (var s in sprites)
            {
                try
                {
                    var idx = int.Parse(s.name["barrel_".Length..]);
                    indexToSprite.TryGetValue(idx, out var lookupName);
                    if (lookupName != null)
                    {
                        spriteDictionary.Add(lookupName, s);
                    }
                }
                catch (Exception) 
                {
                    Debug.LogWarning($"Could not import sprite {s.name}");
                }
            }
            spritesInitialized = true;
        }
        
        sprite = GetComponent<SpriteRenderer>();
    }
    
    private void FixedUpdate()
    {
        if (framesSinceLastBarrelUpdate > FramesBetweenBarrelUpdate)
        {
            barrelFrame++;
            if (barrelFrame > spriteDictionary.Count - 1) barrelFrame = 0;
            framesSinceLastBarrelUpdate = 0;
        }
        framesSinceLastBarrelUpdate++;

        var newSprite = $"barrel{barrelFrame}";
        if (currentSprite != newSprite)
        {
            SwapSprite(newSprite);
            currentSprite = newSprite;
        }
    }
    
    private void SwapSprite(string newSpriteName)
    {
        spriteDictionary.TryGetValue(newSpriteName, out var newSprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Barrel sprite {newSpriteName} could not be found!");
            return;
        }
        sprite.sprite = newSprite;
    }
}