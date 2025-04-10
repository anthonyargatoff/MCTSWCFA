﻿using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRewindController: MonoBehaviour, ICreationObserver<Rewindable>
{
    [SerializeField] private LineRenderer sceneLineRenderer;

    private List<Rewindable> trackedRewindables = new();
    
    private Camera mainCamera;
    private Texture2D rewindCursor;
    private Vector2 cursorHotspot;
    
    public bool IsRewinding { get; private set; }
    public bool RewindActive { get; private set; }

    public UnityEvent<bool> OnRewindToggle { get; } = new();

    private Rewindable rewoundObject;
    private Rewindable focusRewindable;
    private ParticleSystem hitParticles;
    private ParticleSystem lineParticles;
    
    private PlayerController playerController;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        var rewindIndicator = transform.Find("RewindIndicator");
        hitParticles = rewindIndicator?.transform.Find("HitParticles")?.GetComponent<ParticleSystem>();
        lineParticles = rewindIndicator?.transform.Find("LineParticles")?.GetComponent<ParticleSystem>();
        
        rewindCursor = Resources.Load("Sprites/RewindCursor") as Texture2D;
        if (rewindCursor)
        {
            cursorHotspot = new Vector2(rewindCursor.width / 2f, rewindCursor.height / 2f);
        }

        playerController = GetComponent<PlayerController>();
        playerController.OnDeath += () =>
        {
            if (rewoundObject) rewoundObject.CancelRewind();
            CleanupRewind();
        };
        GameManager.OnPaused += CleanupRewind;
        GameManager.OnLevelCompleted += CleanupRewind;
        
        ICreationObservable<Rewindable>.Subscribe(this);
    }

    private void Start()
    {
        var allRewindables = FindObjectsByType<Rewindable>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach (var rewindable in allRewindables)
        {
            OnObservableCreated(rewindable);
        }
    }

    private void CleanupRewind()
    {
        if (!IsRewinding) return;
        IsRewinding = false;
        Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
    }
    
    private void OnDestroy()
    {
        ICreationObservable<Rewindable>.Unsubscribe(this);
    }

    private void OnToggleRewind()
    {
        if (playerController.IsDead || !AllowedToRewind())
        {
            if (IsRewinding) OnRewindToggle.Invoke(false);
            CleanupRewind();
            return;
        }
        
        if (RewindActive)
        {
            rewoundObject.CancelRewind();
        }
        else
        {
            IsRewinding = !IsRewinding;
            
            if (IsRewinding)
            {
                Cursor.SetCursor(rewindCursor, cursorHotspot, CursorMode.Auto);
                Cursor.visible = true;
            }
            else
            {
                Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
            }
            
            OnRewindToggle.Invoke(IsRewinding);
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, IsRewinding ? 0 : 1, 1f).SetEase(Ease.Linear).SetUpdate(true);
        }
    }
    
    private void Update()
    {
        DrawTargetLines();
        HighlightFocusRewindable();
    }

    private void DrawTargetLines()
    {
        if (!hitParticles || !lineParticles) return;
        if (!IsRewinding || !AllowedToRewind())
        {
            lineParticles.Stop();
            hitParticles.Stop();
            return;
        }
        
        
        var pos = new Vector3[2];
        pos[0] = transform.position;
        if (focusRewindable)
        {
            pos[1] = focusRewindable.transform.position;
        }
        else
        {
            var mousePos = GetMousePositionInWorldCoords();
            var hit = MousePositionRaycast(Vector2.Distance(pos[0], mousePos));
            pos[1] = hit.transform? hit.point : mousePos;
        }
        
        var dis = Vector2.Distance(pos[0], pos[1]);
        var angle = Mathf.Atan2(pos[1].y - pos[0].y, pos[1].x - pos[0].x) * 180 / Mathf.PI;

        var absScale = new Vector3(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z)
        );
        var worldToTransformMatrix = Matrix4x4.TRS(transform.position,transform.rotation, absScale).inverse;
        
        var lineShape = lineParticles.shape;
        lineShape.position = worldToTransformMatrix.MultiplyPoint3x4(Vector3.Lerp(pos[0], pos[1], 0.5f));
        lineShape.radius = dis / 2;
        lineShape.rotation = new Vector3(0, 0, angle);
       
        var shape = hitParticles.shape;
        
        shape.position = worldToTransformMatrix.MultiplyPoint3x4(pos[1]);

        Collider2D col = null;
        
        var emission = hitParticles.emission;
        focusRewindable?.TryGetComponent(out col);
        if (col)
        {
            shape.shapeType = col switch
            {
                CircleCollider2D or CapsuleCollider2D or PolygonCollider2D => ParticleSystemShapeType.Circle,
                BoxCollider2D or EdgeCollider2D => ParticleSystemShapeType.Box,
                _ => shape.shapeType
            };
            shape.scale = new Vector3(1.5f * col.bounds.size.x, 1.5f * col.bounds.size.y, 1);
            emission.rateOverTime = Mathf.RoundToInt((shape.scale.x + shape.scale.y) * 12.5f);
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.scale = Vector3.one;
            emission.rateOverTime = 25;
        }

        var lineMain = lineParticles.main;
        lineMain.useUnscaledTime = true;
        var main = hitParticles.main;
        main.useUnscaledTime = true;
        
        if (!lineParticles.isPlaying) lineParticles.Play();
        if (!hitParticles.isPlaying) hitParticles.Play();
    }
    
    private void HighlightFocusRewindable()
    {
        if (focusRewindable == null || !IsRewinding || RewindActive || !AllowedToRewind())
        {
            sceneLineRenderer.positionCount = 0;
            return;
        }
        
        var rewindableSnapshots = focusRewindable.GetSnapshots();
        if (rewindableSnapshots.Count < 2)
        {
            sceneLineRenderer.positionCount = 0;
            return;
        }
        
        sceneLineRenderer.positionCount = rewindableSnapshots.Count;
        var positions = new Vector3[sceneLineRenderer.positionCount];
        for (var i = 0; i < rewindableSnapshots.Count; i++)
        {
            var snapshot = rewindableSnapshots[i];
            positions[i] = snapshot.Position;
        }
        sceneLineRenderer.SetPositions(positions);
    }

    private IEnumerator OnRewindableSelected(Rewindable rewindable)
    {
        if (rewindable.IsDestroyed() || RewindActive || !IsRewinding || !AllowedToRewind()) yield break;
        if (!(focusRewindable?.Equals(rewindable) ?? false) && IsMouseTargetOccluded(Vector2.Distance(transform.position,rewindable.transform.position),rewindable)) yield break;
        
        rewoundObject = rewindable;
        OnToggleRewind();
        AudioManager.PlaySound(Audios.Rewind);
        RewindActive = true;

        yield return rewindable.Rewind();
        
        RewindActive = false;
        rewoundObject = null;
    }

    private void SetFocusRewindable(Rewindable rewindable)
    {
        if (!IsRewinding || !AllowedToRewind() || rewindable.IsDestroyed()) return;
        if (IsMouseTargetOccluded(Vector2.Distance(transform.position,rewindable.transform.position),rewindable)) return;
        focusRewindable = rewindable;
    }

    private void RemoveFocusRewindable(Rewindable rewindable)
    {
        if (!AllowedToRewind()) return;
        focusRewindable = focusRewindable != null && focusRewindable.Equals(rewindable) ? null : focusRewindable;
    }

    public void OnObservableCreated(Rewindable obj)
    {
        if (trackedRewindables.Contains(obj)) return;
        trackedRewindables.Add(obj);
        obj.onMouseDown.AddListener(() => StartCoroutine(OnRewindableSelected(obj)));
        obj.onMouseEnter.AddListener(() => SetFocusRewindable(obj));
        obj.onMouseExit.AddListener(() => RemoveFocusRewindable(obj));
    }

    public void OnObservableDestroyed(Rewindable obj)
    {
        trackedRewindables.Remove(obj);
        if (obj.Equals(rewoundObject))
        {
            rewoundObject.CancelRewind();
        }
    }

    private bool IsMouseTargetOccluded(float objectDistance, Rewindable obj = null)
    {
        var hit = MousePositionRaycast(objectDistance + 1f);
        
        var hitTooClose = Vector2.Distance(transform.position, hit.point) < objectDistance;
        if (obj && hit.transform)
        {
            return !obj.transform.Equals(hit.transform) && !hit.transform.IsChildOf(obj.transform);
        }
        return hit.transform  && hitTooClose;
    }

    private RaycastHit2D MousePositionRaycast(float distance = 100)
    {
        var mousePos = GetMousePositionInWorldCoords();
        return Physics2D.Raycast(transform.position, (mousePos - transform.position).normalized, distance, LayerMask.GetMask("Ground","Object","StageElement"));
    }

    private Vector3 GetMousePositionInWorldCoords()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 1;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private bool AllowedToRewind()
    {
        return !playerController.IsDead && !GameManager.isGamePaused && !GameManager.isCompletingLevel && !GameManager.isStartingLevel;
    }
}