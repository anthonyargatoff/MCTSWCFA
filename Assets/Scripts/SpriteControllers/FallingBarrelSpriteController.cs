using System;
using System.Collections.Generic;
using UnityEngine;

public class FallingBarrelSpriteController: SpriteControllerMonoBehaviour
{
    private static bool _spritesInitialized;
    private static readonly Dictionary<int, string> IndexToSprite = new()
    {
        [1] = "barrel0",
        [2] = "barrel1",
        [3] = "barrel2",
        [8] = "s_barrel0",
        [9] = "s_barrel1",
        [10] = "s_barrel2",
    };
    private static readonly Dictionary<string,Sprite> SpriteDictionary = new();

    private int barrelFrame;
    private const int FramesBetweenBarrelUpdate = 15;
    private int framesSinceLastBarrelUpdate = 0;
    
    private FallingBarrel fallingBarrel;
    
    private new void Awake()
    {
        base.Awake();
        fallingBarrel = GetComponent<FallingBarrel>();
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        InitializeSpriteDictionary("barrel", IndexToSprite, SpriteDictionary);
        _spritesInitialized = true;
    }
    
    private void FixedUpdate()
    {
        if (framesSinceLastBarrelUpdate > GameManager.GetScaledFrameCount(FramesBetweenBarrelUpdate))
        {
            barrelFrame++;
            if (barrelFrame > 2) barrelFrame = 0;
            framesSinceLastBarrelUpdate = 0;
        }
        framesSinceLastBarrelUpdate++;

        var newSprite = $"{(fallingBarrel.IsSpecial ? "s_" : "")}barrel{barrelFrame}";
        if (CurrentSprite != newSprite)
        {
            SwapSprite(newSprite);
            CurrentSprite = newSprite;
        }
    }

    protected override void HandleSpriteSwap()
    {
        var newSprite = $"{(fallingBarrel.IsSpecial ? "s_" : "")}barrel{barrelFrame}";
        if (CurrentSprite != newSprite)
        {
            SwapSprite(newSprite);
            CurrentSprite = newSprite;
        }
    }
    
    protected override void SwapSprite(string newSpriteName)
    {
        SpriteDictionary.TryGetValue(newSpriteName, out var newSprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Barrel sprite {newSpriteName} could not be found!");
            return;
        }
        Sprite.sprite = newSprite;
    }
}