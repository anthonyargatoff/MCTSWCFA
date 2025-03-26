using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpriteController: MonoBehaviour
{
    private static bool spritesInitialized = false;
    private static Dictionary<int, string> indexToSprite = new()
    {
        [0] = "idle",
        [1] = "walk0",
        [2] = "walk1",
        [3] = "jump",
        [4] = "idle_ladder",
        [5] = "climb0",
        [6] = "climb1",
        [7] = "climbover_0",
        [8] = "climbover_1",
        [9] = "win",
        [10] = "idle_hammer0",
        [11] = "walk0_hammer0",
        [12] = "walk1_hammer0",
        [13] = "idle_hammer1",
        [14] = "walk0_hammer1",
        [15] = "walk1_hammer1",
        [16] = "idle_timewarp",
        [17] = "dead1",
        [18] = "dead0",
        [19] = "hammer0",
        [20] = "hammer1",
        [21] = "timewarp",
    };
    private static Dictionary<string,Sprite> spriteDictionary = new();

    private PlayerController controller;
    private PlayerRewindController rewindController;
    private SpriteRenderer sprite;
    private string currentSprite = string.Empty;
    
    private bool previousClimbing;
    private bool dyingAnimationPlayed;
    private bool dyingAnimationFinished;
    private bool playingClimbOver;

    private bool climbFrame;
    private bool walkFrame;
    private bool hammerFrame;

    private GameObject hammerSpriteGameObject;
    private GameObject timewarpSpriteGameObject;
    private SpriteRenderer hammerSprite;
    private SpriteRenderer timewarpSprite;

    private const int FramesBetweenWalkUpdate = 30;
    private int framesSinceLastWalkUpdate = 0;
    
    private const int FramesBetweenHammerUpdate = 60;
    private int framesSinceLastHammerUpdate = 0;
    
    private const int FramesBetweenClimbUpdate = 30;
    private int framesSinceLastClimbUpdate = 0;

    private float lastY = 0;

    private void Awake()
    {
        if (!spritesInitialized)
        {
            var sprites = Resources.LoadAll<Sprite>("Sprites/player");
            foreach (var s in sprites)
            {
                try
                {
                    var idx = int.Parse(s.name["player_".Length..]);
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
        
        controller = GetComponent<PlayerController>();
        controller.OnDeath += () =>
        {
            if (!dyingAnimationPlayed)
            {
                StartCoroutine(PlayDyingAnimation());
            }
        };
        
        rewindController = GetComponent<PlayerRewindController>();
        sprite = transform.Find("PlayerModel")?.GetComponent<SpriteRenderer>();
     
        hammerSpriteGameObject = new GameObject("HammerSprite");
        hammerSpriteGameObject.transform.SetParent(transform);
        
        timewarpSpriteGameObject = new GameObject("TimewarpSprite");
        timewarpSpriteGameObject.transform.SetParent(transform);
        timewarpSpriteGameObject.transform.localPosition = new Vector3(0, 1, 0);
        
        hammerSprite = hammerSpriteGameObject.AddComponent<SpriteRenderer>();
        hammerSprite.enabled = false;
        timewarpSprite = timewarpSpriteGameObject.AddComponent<SpriteRenderer>();
        timewarpSprite.enabled = false;
        if (spriteDictionary.TryGetValue("timewarp", out var twSprite))
        {
            timewarpSprite.sprite = twSprite;
        }
    }

    private void Update()
    {
        hammerSpriteGameObject.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x),1,1);
        HandleSpriteSwap();
        
        if (framesSinceLastWalkUpdate > FramesBetweenWalkUpdate)
        {
            walkFrame = controller.isWalking && !walkFrame;
            framesSinceLastWalkUpdate = 0;
        }
        
        if (framesSinceLastHammerUpdate > FramesBetweenHammerUpdate)
        {
            hammerFrame = controller.UsingHammer && !hammerFrame;
            framesSinceLastHammerUpdate = 0;
        }
        
        if (framesSinceLastClimbUpdate > FramesBetweenClimbUpdate)
        {
            climbFrame = controller.isClimbing && !climbFrame;
            framesSinceLastClimbUpdate = 0;
        }

        framesSinceLastWalkUpdate++;
        framesSinceLastHammerUpdate++;
        framesSinceLastClimbUpdate++;
    }

    private void HandleSpriteSwap()
    {
        // Special Animations
        
        // Death
        if (controller.isDead)
        {
            return;
        }
        
        // Rewind State
        if (rewindController.IsRewinding)
        {
            if (!currentSprite.Equals("idle_timewarp"))
            {
                currentSprite = "idle_timewarp";
                SwapSprite(currentSprite);
            }
            timewarpSprite.enabled = true;
            return;
        }
        timewarpSprite.enabled = false;
        
        var nextSprite = "idle";
        if (controller.isClimbing)
        {
            if (controller.atLadderBottom)
            {
                nextSprite = "idle_ladder";
            } else
            {
                var movedY = Mathf.Abs(transform.position.y - lastY) > 0.01f;
                nextSprite = movedY ? $"climb{(climbFrame ? 0 : 1)}" : controller.atLadderTop && controller.IsAboveCurrentLadder() && currentSprite.Contains("climb") ? "idle" : currentSprite;
            }
        } else if (!controller.isGrounded)
        {
            nextSprite = "jump";
        } else if (controller.isWalking)
        {
            nextSprite = $"walk{(walkFrame ? 0 : 1)}";
        }
        
        // Show hammer, but only if pre-requisite animation has relevant state for it
        var hammerName = $"hammer{(hammerFrame ? 0 : 1)}";
        if (controller.UsingHammer && spriteDictionary.ContainsKey($"{nextSprite}_{hammerName}") && spriteDictionary.TryGetValue(hammerName, out var hammerSet))
        {
            hammerSprite.enabled = true;
            nextSprite = $"{nextSprite}_{hammerName}";
            hammerSprite.transform.localPosition = new Vector3(
                hammerFrame ? 0 : 1,
                hammerFrame ? 1 : 0,
                0
            );
            hammerSprite.sprite = hammerSet;
        }
        else
        {
            hammerSprite.enabled = false;
        }

        if (!nextSprite.Equals(currentSprite))
        {
            SwapSprite(nextSprite);
            currentSprite = nextSprite;
        }
        previousClimbing = controller.isClimbing;
        lastY = transform.position.y;
    }

    private IEnumerator PlayDyingAnimation()
    {
        dyingAnimationPlayed = true;
        SwapSprite("dead0");
        
        const float degreesPerSecond = 500f;
        var degreesTravelled = 0f;
        do
        {
            var dg = degreesPerSecond * Time.unscaledDeltaTime;
            degreesTravelled += dg;
            sprite.transform.Rotate(new Vector3(0,0,dg));
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        } while (degreesTravelled < 360f);

        sprite.transform.rotation = Quaternion.identity;
        SwapSprite("dead1");
        yield return new WaitForSeconds(1.0f);
    }

    private void SwapSprite(string newSpriteName)
    {
        spriteDictionary.TryGetValue(newSpriteName, out var newSprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Player sprite {newSpriteName} could not be found!");
            return;
        }
        sprite.sprite = newSprite;
    }
}