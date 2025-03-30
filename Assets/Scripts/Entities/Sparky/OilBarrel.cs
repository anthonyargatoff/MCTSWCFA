
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class OilBarrel: SpriteControllerMonoBehaviour
{
    private static bool _spritesInitialized; 
    private static readonly Dictionary<int, string> IndexToSprite = new()
    {
        [0] = "oil",
        [1] = "fire0",
        [2] = "fire1",
        [3] = "fire2",
        [5] = "inferno0",
        [6] = "inferno1",
        [7] = "inferno2",
    };
    private static readonly Dictionary<string,Sprite> SpriteDictionary = new();
    
    private const int maxSparkies = 3;
    private int sparkies = 0;

    private const float fireUpDuration = 10;
    private float fireUpTimer = 0;
    
    private const float timeBetweenFireUp = 7.5f;
    private float timeSinceLastFireUp = timeBetweenFireUp / 2f;
    private bool isFiringUp;

    private const int fireFrameMax = 2;
    private int fireFrame;

    private const int framesBetweenUpdate = 10;
    private int framesSinceLastUpdate;

    private bool readyToSpawn;
    private static GameObject _sparkyPrefab;
    
    private new void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        if (_spritesInitialized) return;
        _sparkyPrefab = Resources.Load<GameObject>("Prefabs/Entities/Sparky");
        InitializeSpriteDictionary("oil_barrel", IndexToSprite, SpriteDictionary);
        _spritesInitialized = true;
    }

    private void Update()
    {
        if (Time.deltaTime > 0) {
            if (framesSinceLastUpdate > GameManager.GetScaledFrameCount(framesBetweenUpdate))
            {
                fireFrame++;
                if (fireFrame > fireFrameMax) fireFrame = 0;
                framesSinceLastUpdate = 0;
            }
            framesSinceLastUpdate++;
        }
        
        if (fireUpTimer > fireUpDuration)
        {
            isFiringUp = false;
            fireUpTimer = 0;
        }
        if (timeSinceLastFireUp > timeBetweenFireUp)
        {
            isFiringUp = true;
            readyToSpawn = true;
            timeSinceLastFireUp = 0;
        }
        if (!isFiringUp) timeSinceLastFireUp += Time.deltaTime;
        else fireUpTimer += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        HandleSpriteSwap();
        if (readyToSpawn && isFiringUp && fireUpTimer > fireUpDuration / 2)
        {
            readyToSpawn = false;
            StartCoroutine(SpawnSparky());
        }
    }

    protected override void HandleSpriteSwap()
    {
        var newSprite = $"{(isFiringUp ? "inferno" : "fire")}{fireFrame}";
        if (!CurrentSprite.Equals(newSprite))
        {
            CurrentSprite = newSprite;
            SwapSprite(CurrentSprite);
        }
    }

    protected override void SwapSprite(string newSpriteName)
    {
        base.SwapSprite(newSpriteName, SpriteDictionary);
    }

    private IEnumerator SpawnSparky()
    {
        if (sparkies >= maxSparkies) yield break;
        var s = Instantiate(_sparkyPrefab, transform.position, Quaternion.identity);
        sparkies++;
        var sparky = s.GetComponent<SparkyController>();
        if (!sparky) yield break;
        sparky.isBeingSpawned = true;
        
        var closestGround = Physics2D.Raycast(new Vector2(transform.position.x,transform.position.y), Vector2.down, 3f, LayerMask.GetMask("Ground")).collider;
        var goalY = transform.position.y - 2f;
        if (closestGround)
        {
            goalY = closestGround.bounds.center.y + 0.5f;
        }
        var goalPosition = new Vector2(transform.position.x + (Random.Range(0,2) == 0 ? 1 : -1) * Random.Range(2f,5f), goalY);
        
        var control = Vector3.Lerp(transform.position, goalPosition, 0.5f) + (Vector3.up * 2f);
        var points = QuadraticBezier.GenerateBezierCurvePath(transform.position, goalPosition, control, 10);
        
        var seq = DOTween.Sequence();
        foreach (var point in points)
        {
            seq.Append(sparky.transform.DOMove(point,0.1f).SetEase(Ease.Linear));
        }
        seq.SetLink(sparky.gameObject).Play();
        yield return new WaitForSeconds(seq.Duration());
        sparky.isBeingSpawned = false;
    }
}