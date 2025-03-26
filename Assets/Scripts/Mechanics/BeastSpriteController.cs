using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BeastSpriteController: MonoBehaviour
{
    private static bool spritesInitialized = false;
    private static Dictionary<int, string> indexToSprite = new()
    {
        [0] = "idle",
        [1] = "walk0",
        [2] = "walk1",
        [3] = "barrel0",
        [4] = "barrel1",
        [5] = "barrel2",
    };
    private static Dictionary<string,Sprite> spriteDictionary = new();
    private static Dictionary<string,Sprite> overlaySpriteDictionary = new();
    
    private SpriteRenderer sprite;
    private SpriteRenderer overlaySprite;
    
    private BarrelSpawner barrelSpawner;
    private DonkeyKong donkeyKong;
    private Rigidbody2D rb;

    private string currentSprite;
    
    private bool walkFrame;
    private const int FramesBetweenWalkUpdate = 30;
    private int framesSinceLastWalkUpdate = 0;

    private int barrelThrowsQueued = 0;
    private bool playingBarrelDropAnim;
    
    private void Awake()
    {
        if (!spritesInitialized)
        {
            var sprites = Resources.LoadAll<Sprite>("Sprites/beast");
            foreach (var s in sprites)
            {
                try
                {
                    var idx = int.Parse(s.name["beast_".Length..]);
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
            
            sprites = Resources.LoadAll<Sprite>("Sprites/beast_overlay");
            foreach (var s in sprites)
            {
                try
                {
                    var idx = int.Parse(s.name["beast_overlay_".Length..]);
                    indexToSprite.TryGetValue(idx, out var lookupName);
                    if (lookupName != null)
                    {
                        overlaySpriteDictionary.Add(lookupName, s);
                    }
                }
                catch (Exception) 
                {
                    Debug.LogWarning($"Could not import sprite {s.name}");
                }
            }

            spritesInitialized = true;
        }
        
        barrelSpawner = GetComponent<BarrelSpawner>();
        if (barrelSpawner)
        {
            barrelSpawner.OnBarrel += (direction, until) => StartCoroutine(OnBarrelThrow(direction,until));
        }
        donkeyKong = GetComponent<DonkeyKong>();
        if (donkeyKong)
        {
            donkeyKong.OnBarrel += (until) => StartCoroutine(OnBarrelDrop(until));
        }
        
        rb = GetComponent<Rigidbody2D>();
        
        sprite = GetComponent<SpriteRenderer>();
        overlaySprite = transform.Find("Overlay").GetComponent<SpriteRenderer>();
    }
    
    private void FixedUpdate()
    {
        HandleSpriteSwap();
        
        if (framesSinceLastWalkUpdate > FramesBetweenWalkUpdate)
        {
            walkFrame = rb && Mathf.Abs(rb.linearVelocityX) > 0.01f && !walkFrame;
            framesSinceLastWalkUpdate = 0;
        }
        
        framesSinceLastWalkUpdate++;
    }

    private void HandleSpriteSwap()
    {
        if (barrelThrowsQueued > 0 || playingBarrelDropAnim) return;
        
        var nextSprite = "idle";
        var ran = new Random();
        
        if (donkeyKong)
        {
            if (rb && Mathf.Abs(rb.linearVelocityX) > 0.01f)
            {
                nextSprite = $"walk{(walkFrame ? 1 : 0)}";
            }
        } 
        
        if (!nextSprite.Equals(currentSprite))
        {
            SwapSprite(nextSprite);
            currentSprite = nextSprite;
        }
    }

    private IEnumerator OnBarrelThrow(BarrelSpawner.ThrowDirection dir, float waitForSeconds)
    {
        barrelThrowsQueued++;
        var first = dir == BarrelSpawner.ThrowDirection.Left ? 0 : 2;
        var third = dir == BarrelSpawner.ThrowDirection.Left ? 2 : 0;
        var dt = waitForSeconds / 3;
        SwapSprite($"barrel{first}");
        yield return new WaitForSeconds(dt);
        SwapSprite("barrel1");
        yield return new WaitForSeconds(dt);
        SwapSprite($"barrel{third}");
        yield return new WaitForSeconds(dt);
        barrelThrowsQueued--;
    }

    private IEnumerator OnBarrelDrop(float waitForSeconds)
    {
        yield return new WaitForSeconds(waitForSeconds - 0.2f);
        playingBarrelDropAnim = true;
        SwapSprite("barrel1");
        yield return new WaitForSeconds(0.2f);
        playingBarrelDropAnim = false;
    }
    
    private void SwapSprite(string newSpriteName)
    {
        spriteDictionary.TryGetValue(newSpriteName, out var newSprite);
        overlaySpriteDictionary.TryGetValue(newSpriteName, out var newOverlaySprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Beast sprite {newSpriteName} could not be found!");
            return;
        }
        sprite.sprite = newSprite;
        if (overlaySprite) 
            overlaySprite.sprite = newOverlaySprite;
    }
}