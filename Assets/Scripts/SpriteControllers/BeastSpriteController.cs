using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BeastSpriteController: SpriteControllerMonoBehaviour
{
    private static bool _spritesInitialized;
    private static readonly Dictionary<int, string> IndexToSprite = new()
    {
        [0] = "idle",
        [1] = "walk0",
        [2] = "walk1",
        [3] = "barrel0",
        [4] = "barrel1",
        [5] = "barrel2",
    };
    private static readonly Dictionary<string,Sprite> SpriteDictionary = new();
    private static readonly Dictionary<string,Sprite> OverlaySpriteDictionary = new();

    [SerializeField] private GameObject floor;
    
    private SpriteRenderer overlaySprite;
    
    private BarrelSpawner barrelSpawner;
    private DonkeyKong donkeyKong;
    private Rigidbody2D rb;

    private string currentSprite;
    
    private bool walkFrame;
    private const int FramesBetweenWalkUpdate = 10;
    private int framesSinceLastWalkUpdate = 0;

    private int barrelThrowsQueued = 0;
    private bool playingBarrelDropAnim;
    private bool isPlayingEndAnim;

    private PlayerRewindController rewindController;
    
    private new void Awake()
    {
        base.Awake();
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
        overlaySprite = transform.Find("Overlay").GetComponent<SpriteRenderer>();
        
        var candidates = FindObjectsByType<PlayerRewindController>(FindObjectsSortMode.InstanceID);
        if (candidates.Length > 0)
        {
            rewindController = candidates[0];
        }
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        InitializeSpriteDictionary("beast", IndexToSprite, SpriteDictionary);
        InitializeSpriteDictionary("beast_overlay", IndexToSprite, OverlaySpriteDictionary);
        _spritesInitialized = true;
    }
    
    private void Update()
    {
        if (GameManager.isGamePaused || isPlayingEndAnim || (rewindController?.IsRewinding ?? false)) return;
        HandleSpriteSwap();
        
        if (framesSinceLastWalkUpdate > GameManager.GetScaledFrameCount(FramesBetweenWalkUpdate))
        {
            walkFrame = rb && Mathf.Abs(rb.linearVelocityX) > 0.01f && !walkFrame;
            framesSinceLastWalkUpdate = 0;
        }
        
        framesSinceLastWalkUpdate++;
    }

    protected override void HandleSpriteSwap()
    {
        if (barrelThrowsQueued > 0 || playingBarrelDropAnim) return;
        
        var nextSprite = "idle";
        
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
        yield return new WaitForSecondsRealtime(dt);
        SwapSprite("barrel1");
        yield return new WaitForSecondsRealtime(dt);
        SwapSprite($"barrel{third}");
        yield return new WaitForSecondsRealtime(dt);
        barrelThrowsQueued--;
    }

    private IEnumerator OnBarrelDrop(float waitForSeconds)
    {
        yield return new WaitForSecondsRealtime(waitForSeconds - 0.2f);
        playingBarrelDropAnim = true;
        SwapSprite("barrel1");
        yield return new WaitForSecondsRealtime(0.2f);
        playingBarrelDropAnim = false;
    }
    
    protected override void SwapSprite(string newSpriteName)
    {
        SpriteDictionary.TryGetValue(newSpriteName, out var newSprite);
        OverlaySpriteDictionary.TryGetValue(newSpriteName, out var newOverlaySprite);
        if (!newSprite)
        {
            Debug.LogWarning($"Beast sprite {newSpriteName} could not be found!");
            return;
        }
        Sprite.sprite = newSprite;
        if (overlaySprite) 
            overlaySprite.sprite = newOverlaySprite;
    }
    
    public IEnumerator StartEndAnimation()
    {
        if (barrelSpawner)
        {
            barrelSpawner.enabled = false;
        }
        if (donkeyKong)
        {
            donkeyKong.enabled = false;
        }

        PrincessSpriteController princessSprite;
        var candidates = FindObjectsByType<PrincessSpriteController>(FindObjectsSortMode.InstanceID);
        if (candidates.Length > 0)
        {
            princessSprite = candidates[0];
        }
        else yield break;

        var env = GameObject.Find("Environment");
        var bp = env?.transform?.Find("BeastEndPath");
        if (!env || !bp) yield break;

        var tween = DOTween.Sequence();
        var npoints = bp.childCount;
        var curPos = transform.position;
        for (var i = 0; i <= npoints; i++)
        {
            var p = i < npoints ? bp.GetChild(i) : null;
            var goalPos = i < npoints ? p!.transform.position : princessSprite.transform.position;
            
            var xDis = Vector2.Distance(new Vector2(goalPos.x,0), new Vector2(curPos.x, 0));
            if (Mathf.Abs(goalPos.y - curPos.y) > 0.1f)
            {
                var center = Vector2.Lerp(goalPos, curPos, 0.5f);
                var nPoints = (int)(1 + xDis) * 10;
                var positions = QuadraticBezier.GenerateBezierCurvePath(curPos, goalPos, new Vector3(center.x,Mathf.Max(curPos.y, goalPos.y) + 3f,0),nPoints);
                
                foreach (var point in positions)
                {
                    tween.Append(transform.DOMove(point, 1f / nPoints).SetEase(Ease.Linear));
                }
            }
            else
            {
                tween.Append(transform.DOMoveX(goalPos.x, xDis / 3f).SetEase(Ease.Linear));
            }
            curPos = goalPos;
        }
        tween.Append(transform.DOMove(princessSprite.transform.position,0.1f).SetEase(Ease.Linear));
        tween.SetUpdate(true).SetLink(gameObject).Play();
        var frame = false;
        while (tween.IsActive())
        {
            frame = !frame;
            SwapSprite($"walk{(frame ? 1 : 0)}");
            yield return new WaitForSecondsRealtime(0.2f);
        }
        
        SwapSprite("barrel1");
        princessSprite.ToggleBeastHold();
        yield return new WaitForSecondsRealtime(0.5f);

        var distanceFromTop = 10f - transform.position.y;
        transform.DOMoveY(transform.position.y + distanceFromTop + 1f, 0.5f).SetEase(Ease.OutCubic).SetLink(gameObject).SetUpdate(true);
        princessSprite.transform.DOMoveY(princessSprite.transform.position.y + distanceFromTop + 1f, 0.5f).SetEase(Ease.OutCubic).SetLink(princessSprite.gameObject).SetUpdate(true);
    }

    public IEnumerator StartFinalAnimation()
    {
        if (barrelSpawner)
        {
            barrelSpawner.enabled = false;
        }
        if (donkeyKong)
        {
            donkeyKong.enabled = false;
        }
        
        var cam = Camera.main!;
        var goalY = -cam.orthographicSize * cam.aspect;
        var time = Mathf.Abs(transform.position.x) / 5f; 
        transform.DOMoveX(0, time).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(time);
        floor?.transform.DOMoveY(goalY,1f).SetEase(Ease.InCubic).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.5f);
        transform.DOLocalRotate(new Vector3(0f,0f,180f), 0.2f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.2f);
        
        var vel = 0f;
        
        var beenHit = new List<Collider2D>();
        while (transform.position.y > goalY)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, 1.5f, LayerMask.GetMask("Ground")).ToList();
            var validHits = hits.Where(h => !beenHit.Contains(h)).ToList();
            foreach (var hit in validHits)
            {
                if (hit.transform.position.y > transform.position.y) continue;
                hit.transform.DOMoveY(goalY, 1f).SetEase(Ease.InCubic).SetUpdate(true);
                AudioManager.PlaySound(Audios.Destroy, volume: 0.25f);
                vel += 0.09f;
            }
            beenHit.AddRange(validHits);
            
            vel += -9.8f * 0.01f;
            transform.position += new Vector3(0,vel * 0.01f,0); 
            yield return new WaitForSecondsRealtime(0.01f);
        }
        AudioManager.PlaySound(Audios.Destroy, volume: 1.0f, pitch: 0.95f);
        yield return new WaitForSecondsRealtime(1.5f);
    }
}