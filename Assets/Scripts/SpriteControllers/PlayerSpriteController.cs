using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerSpriteController: SpriteControllerMonoBehaviour
{
    [SerializeField] private bool isInVictoryScene;

    private static bool _spritesInitialized; 
    private static readonly Dictionary<int, string> IndexToSprite = new()
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
        [19] = "win_blush",
        [20] = "hammer0",
        [21] = "hammer1",
        [22] = "timewarp",
        [26] = "idle_blush",
        [27] = "win_jump",
    };
    private static readonly Dictionary<string,Sprite> SpriteDictionary = new();

    private PlayerController controller;
    private PlayerRewindController rewindController;
    private Rigidbody2D rb;
    
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

    private const int FramesBetweenWalkUpdate = 10;
    private int framesSinceLastWalkUpdate = 0;
    
    private const int FramesBetweenHammerUpdate = 15;
    private int framesSinceLastHammerUpdate = 0;

    private const int FramesBetweenClimbUpdate = 5;
    private int framesSinceLastClimbUpdate = 0;

    private float lastY = 0;

    private const string RunPfx = "RunParticles";
    private const string LandPfx = "LandParticles";
    private Dictionary<string, ParticleSystem> particles = new();

    private bool lastGrounded;
    
    private new void Awake()
    {
        base.Awake();
        controller = GetComponent<PlayerController>();
        if (controller)
        {
            controller.OnDeath += () =>
            {
                if (!dyingAnimationPlayed)
                {
                    StartCoroutine(PlayDyingAnimation());
                }
            };
        }

        rewindController = GetComponent<PlayerRewindController>();
        Sprite = transform.Find("PlayerModel")?.GetComponent<SpriteRenderer>();
     
        hammerSpriteGameObject = new GameObject("HammerSprite");
        hammerSpriteGameObject.transform.SetParent(transform);
        
        timewarpSpriteGameObject = new GameObject("TimewarpSprite");
        timewarpSpriteGameObject.transform.SetParent(transform);
        timewarpSpriteGameObject.transform.localPosition = new Vector3(0, 1, 0);
        
        hammerSprite = hammerSpriteGameObject.AddComponent<SpriteRenderer>();
        hammerSprite.enabled = false;
        timewarpSprite = timewarpSpriteGameObject.AddComponent<SpriteRenderer>();
        timewarpSprite.enabled = false;
        if (SpriteDictionary.TryGetValue("timewarp", out var twSprite))
        {
            timewarpSprite.sprite = twSprite;
        }

        var particlesObject = transform.Find("Particles");
        if (particlesObject && particlesObject.childCount > 0)
        {
            for (var i = 0; i < particlesObject.childCount; i++)
            {
                var child = particlesObject.GetChild(i);
                child.TryGetComponent<ParticleSystem>(out var pfx);
                if (pfx)
                    particles.Add(child.name, pfx);
            }
        }
        
        TryGetComponent(out rb);
        if (isInVictoryScene) StartCoroutine(VictorySceneAnimation());
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        InitializeSpriteDictionary("player", IndexToSprite, SpriteDictionary);
        _spritesInitialized = true;
    }
    
    private void Update()
    {
        if (isInVictoryScene) return;
        
        HandleSpriteSwap();
        
        particles.TryGetValue(RunPfx, out var runPfx);
        if (runPfx && rb)
        {
            var emi = runPfx.emission;
            var rot = emi.rateOverTime;
            if (controller.IsWalking)
            {
                rot.constant = Mathf.Abs(Mathf.Round(rb.linearVelocityX * 10));   
            }
            else
            {
                rot.constant = 0;
            }
            emi.rateOverTime = rot;
        }
        
        if (controller.IsGrounded && !lastGrounded)
        {
            TriggerParticles(LandPfx);
        } 
        
        if (framesSinceLastWalkUpdate > GameManager.GetScaledFrameCount(FramesBetweenWalkUpdate))
        {
            walkFrame = controller.IsWalking && !walkFrame;
            framesSinceLastWalkUpdate = 0;
        }
        
        if (framesSinceLastHammerUpdate > GameManager.GetScaledFrameCount(FramesBetweenHammerUpdate))
        {
            hammerFrame = controller.UsingHammer && !hammerFrame;
            framesSinceLastHammerUpdate = 0;
        }
        
        if (framesSinceLastClimbUpdate > GameManager.GetScaledFrameCount(FramesBetweenClimbUpdate))
        {
            climbFrame = controller.IsClimbing && !climbFrame;
            framesSinceLastClimbUpdate = 0;
        }

        framesSinceLastWalkUpdate++;
        framesSinceLastHammerUpdate++;
        framesSinceLastClimbUpdate++;
        
        lastGrounded = controller.IsGrounded;
    }

    protected override void HandleSpriteSwap()
    {
        // Special Animations
        
        // Death
        if (controller.IsDead)
        {
            timewarpSprite.enabled = false;
            hammerSprite.enabled = false;
            return;
        }

        if (GameManager.isCompletingLevel)
        {
            hammerSprite.enabled = false;
            if (!CurrentSprite.Equals("win"))
            {
                CurrentSprite = "win";
                SwapSprite(CurrentSprite);
            }
            return;
        }
        
        // Rewind State
        if (rewindController.IsRewinding)
        {
            hammerSprite.enabled = false;
            if (!CurrentSprite.Equals("idle_timewarp"))
            {
                CurrentSprite = "idle_timewarp";
                SwapSprite(CurrentSprite);
            }
            timewarpSprite.enabled = true;
            return;
        }
        timewarpSprite.enabled = false;
        
        var nextSprite = "idle";
        if (controller.IsClimbing)
        {
            if (controller.AtLadderBottom)
            {
                nextSprite = "idle_ladder";
            } else
            {
                var movedY = Mathf.Abs(transform.position.y - lastY) > 0.01f;
                nextSprite = movedY ? $"climb{(climbFrame ? 0 : 1)}" : controller.AtLadderTop && controller.IsAboveCurrentLadder() && CurrentSprite.Contains("climb") ? "idle" : CurrentSprite;
            }
        } else if (!controller.IsGrounded)
        {
            nextSprite = "jump";
        } else if (controller.IsWalking)
        {
            nextSprite = $"walk{(walkFrame ? 0 : 1)}";
        }
        
        // Show hammer, but only if pre-requisite animation has relevant state for it
        var hammerName = $"hammer{(hammerFrame ? 0 : 1)}";
        if (controller.UsingHammer && nextSprite.Equals("jump"))
        {
            nextSprite = "walk0";
        }
        
        if (controller.UsingHammer && SpriteDictionary.ContainsKey($"{nextSprite}_{hammerName}") && SpriteDictionary.TryGetValue(hammerName, out var hammerSet))
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

        if (!nextSprite.Equals(CurrentSprite))
        {
            SwapSprite(nextSprite);
            CurrentSprite = nextSprite;
        }
        
        lastY = transform.position.y;
    }

    protected override void SwapSprite(string newSpriteName)
    {
        base.SwapSprite(newSpriteName, SpriteDictionary);
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
            Sprite.transform.Rotate(new Vector3(0,0,dg));
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        } while (degreesTravelled < 360f);

        Sprite.transform.rotation = Quaternion.identity;
        SwapSprite("dead1");
        yield return new WaitForSeconds(1.0f);
    }
    
    private IEnumerator VictorySceneAnimation()
    {
        var candidates = FindObjectsByType<PrincessSpriteController>(FindObjectsSortMode.InstanceID);
        PrincessSpriteController princessSprite;
        if (candidates.Length > 0) princessSprite = candidates[0];
        else yield break;

        bool kissed = false;
        princessSprite.victorySceneKissed.AddListener(() => kissed = true);
        
        SwapSprite("idle_ladder");
        yield return new WaitForSecondsRealtime(3.5f);
        Sprite.transform.localScale = new Vector3(-2, 2, 1);
        SwapSprite("idle");

        do
        {
            yield return null;
        } while(!kissed);
        
        SwapSprite("idle_blush");
        yield return new WaitForSecondsRealtime(1f);
        SwapSprite("win_blush");

        var jumpY = transform.position.y + 3f;
        var fallY = transform.position.y;
        while (!gameObject.IsDestroyed())
        {
            transform.DOMoveY(jumpY, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
            yield return new WaitForSecondsRealtime(0.05f);
            SwapSprite("win_jump");
            yield return new WaitForSecondsRealtime(0.45f);
            transform.DOMoveY(fallY,0.25f).SetEase(Ease.InCubic).SetUpdate(true).SetLink(gameObject);
            yield return new WaitForSecondsRealtime(0.25f);
            SwapSprite("win_blush");
            yield return new WaitForSecondsRealtime(1.5f);
        }
    }

    private void TriggerParticles(string pfxName, bool on = true)
    {
        particles.TryGetValue(pfxName, out var pfxSystem);
        if (pfxSystem)
        {
            if (on)
            { 
                if (!pfxSystem.isPlaying) pfxSystem.Play();
            }
            else
            {
                pfxSystem.Stop();
            }
        }
        else
        {
            Debug.LogWarning($"Player particle system {pfxName} could not be found");
        }
    }
}