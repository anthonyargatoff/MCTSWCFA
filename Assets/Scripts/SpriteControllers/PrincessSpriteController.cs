using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PrincessSpriteController: SpriteControllerMonoBehaviour
{
    [SerializeField] private bool isInVictoryScene;
    
    private static bool _spritesInitialized;
    private static readonly Dictionary<int, string> IndexToSprite = new()
    {
        [0] = "idle0",
        [1] = "idle1",
        [2] = "shock",
        [3] = "glee0",
        [4] = "glee1",
        [5] = "yell0",
        [6] = "yell1",
        [7] = "yell2",
        [8] = "forward",
        [9] = "backward",
    };
    private static readonly Dictionary<int, string> IndexToSpriteBig = new()
    {
        [0] = "b_forward",
        [1] = "b_glee0",
        [2] = "b_glee1",
        [3] = "b_carried",
        [4] = "b_kiss0",
        [5] = "b_kiss1",
    };
    private static readonly Dictionary<string,Sprite> SpriteDictionary = new();
    private static readonly Dictionary<string,Sprite> SpriteDictionaryBig = new();

    private PlayerController player;

    private Transform speechBubble;
    private TextMeshProUGUI speechBubbleText;
    
    private bool kissAnimationPlaying;
    
    private const int FramesBetweenUpdate = 20;
    private int framesSinceLastUpdate = 0;
    
    private const int FramesBetweenWalkUpdate = 10;
    private int framesSinceLastWalkUpdate = 0;
    private bool walkFrame;

    private static Dictionary<int, string> yellFrameMap = new()
    {
        [0] = "idle0",
        [1] = "yell1",
        [2] = "yell0",
        [3] = "yell2",
    };
    
    private const int FramesBetweenYellCycle = 120;
    private int framesSinceLastYellCycle;
    private bool inYellCycle;
    private const int YellMax = 3;
    private int yellFrame = 0;

    private bool gleeFrame;
    private bool heldByBeast;
    private bool playerNearby;

    public UnityEvent victorySceneKissed { get; private set; } = new();

    private new void Awake()
    {
        base.Awake();
        var candidates = FindObjectsByType<PlayerController>(FindObjectsSortMode.InstanceID);
        if (candidates.Length > 0) player = candidates[0];
        
        speechBubble = transform.Find("SpeechBubble");
        speechBubbleText = speechBubble?.Find("Canvas").GetComponentInChildren<TextMeshProUGUI>(true);
        
        if (isInVictoryScene) StartCoroutine(VictorySceneAnimation()); 
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        InitializeSpriteDictionary("princess", IndexToSprite, SpriteDictionary);
        InitializeSpriteDictionary("princess_big", IndexToSpriteBig, SpriteDictionaryBig);
        _spritesInitialized = true;
    }
    
    private void Update()
    {
        if (Time.timeScale == 0 || isInVictoryScene) return;
        
        HandleSpriteSwap();

        if (!inYellCycle)
        {
            yellFrame = 0;
        }
        
        if (framesSinceLastUpdate > GameManager.GetScaledFrameCount(FramesBetweenUpdate))
        {
            gleeFrame = !gleeFrame;
            if (inYellCycle)
            {
                yellFrame++;   
            }
            if (yellFrame > YellMax)
            {
                yellFrame = 0;
                inYellCycle = false;
            }
            framesSinceLastUpdate = 0;
        }
        framesSinceLastUpdate++;

        if (framesSinceLastWalkUpdate > GameManager.GetScaledFrameCount(FramesBetweenWalkUpdate))
        {
            walkFrame = !walkFrame;
            framesSinceLastWalkUpdate = 0;
        }
        framesSinceLastWalkUpdate++;

        if (framesSinceLastYellCycle > GameManager.GetScaledFrameCount(FramesBetweenYellCycle))
        {
            inYellCycle = true;
            framesSinceLastYellCycle = 0;
        }
        if (!inYellCycle && !playerNearby) framesSinceLastYellCycle++;
        
        speechBubble?.gameObject.SetActive(inYellCycle && yellFrame is > 1 and <= YellMax);
    }

    protected override void HandleSpriteSwap()
    {
        transform.localScale = new Vector3(1,1,1);

        var newSprite = "idle0";
        if (GameManager.isCompletingLevel)
        {
            newSprite = $"b_glee{(gleeFrame ? 1 : 0)}";
        }

        if (inYellCycle)
        {
            newSprite = yellFrameMap[yellFrame];
        }
        
        if (player)
        {
            var dis = Vector2.Distance(transform.position, player.transform.position);
            if (dis < 4)
            {
                playerNearby = true;
                transform.localScale = new Vector3(transform.position.x > player.transform.position.x ? -1 : 1, 1, 1);
            }
            if (player.IsDead)
            {
                newSprite = "shock";
            } 
        }

        if (CurrentSprite != newSprite)
        {
            SwapSprite(newSprite);
            CurrentSprite = newSprite;
        }

        speechBubbleText.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1, 1);
    }
    
    protected override void SwapSprite(string newSpriteName)
    {
        var isBigSprite = newSpriteName.StartsWith("b_");
        var dictionary = isBigSprite ? SpriteDictionaryBig : SpriteDictionary;
        
        dictionary.TryGetValue(newSpriteName, out var newSprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Princess sprite {newSpriteName} could not be found!");
            return;
        }

        Sprite.sprite = newSprite;
    }

    public void ToggleBeastHold()
    {
        heldByBeast = !heldByBeast;
        CurrentSprite = heldByBeast ? "b_carried" : "idle";
        SwapSprite(CurrentSprite);
    }

    private IEnumerator VictorySceneAnimation()
    {
        var heartParticles = transform.Find("HeartParticles")?.GetComponent<ParticleSystem>();
        var candidates = FindObjectsByType<PlayerSpriteController>(FindObjectsSortMode.InstanceID);
        PlayerSpriteController playerSprite;
        if (candidates.Length > 0) playerSprite = candidates[0];
        else yield break;
        
        SwapSprite("backward");
        yield return new WaitForSecondsRealtime(3f);
        SwapSprite("idle0");
        
        transform.DOMoveX(-0.25f, 2f).SetEase(Ease.Linear).SetUpdate(true);
        var frame = false;
        float t = 0; 
        float tsc = 0;
        while (t < 2f)
        {
            yield return new WaitForSecondsRealtime(0.01f);
            if (tsc > 0.2f)
            {
                frame = !frame;
                SwapSprite($"idle{(frame ? 1 : 0)}");
                tsc = 0f;
            }
            t += 0.01f;
            tsc += 0.01f;
        }
        SwapSprite("b_kiss0");
        yield return new WaitForSecondsRealtime(0.5f);
        SwapSprite("b_kiss1");
        yield return new WaitForSecondsRealtime(0.5f);
        if (heartParticles) heartParticles.Play();
        victorySceneKissed.Invoke();
        SwapSprite("b_kiss0");
        yield return new WaitForSeconds(0.1f);
        transform.DOMoveX(-1.25f, 0.25f).SetEase(Ease.Linear).SetUpdate(true);
        float tm = 1;
        tsc = 0f;
        t = 0f;
        while (t < tm)
        {
            if (tsc > 0.1f)
            {
                frame = !frame;
                SwapSprite($"glee{(frame ? 1 : 0)}");
                tsc = 0f;
            }
            t += Time.unscaledDeltaTime;
            tsc += Time.unscaledDeltaTime;
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }

        SwapSprite("b_forward");
        yield return new WaitForSeconds(0.3f);
        
        tsc = 0f;
        t = 0f;
        frame = false;
        SwapSprite("b_glee0");
        while (!gameObject.IsDestroyed())
        {
            if (tsc > 0.3f)
            {
                frame = !frame;
                SwapSprite($"b_glee{(frame ? 1 : 0)}");
                tsc = 0;
            }
            t += Time.unscaledDeltaTime;
            tsc += Time.unscaledDeltaTime;
            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }
    }
}