using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public record TransformSnapshot
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public float TimeDelta;

    public TransformSnapshot(Transform transform, float timeDelta)
    {
        Position = new Vector3(transform.position.x,transform.position.y,transform.position.z);
        Rotation = new Quaternion(transform.rotation.x,transform.rotation.y,transform.rotation.z,transform.rotation.w);
        Scale = new Vector3(transform.lossyScale.x,transform.lossyScale.y,transform.lossyScale.z);
        TimeDelta = timeDelta;
    }
}

public class Rewindable: MonoBehaviour, ICreationObservable<Rewindable>
{
    private static GameObject _startPfx;
    private static GameObject _endPfx;
    private static GameObject _activePfx;

    private ParticleSystem startPfx;
    private ParticleSystem endPfx;
    private ParticleSystem activePfx;
    
    private readonly List<TransformSnapshot> snapshots = new();
    private float lastUpdate;
    public SpriteRenderer spriteRenderer { get; private set; }
    public bool isRewinding { get; private set; }

    private bool forceCancelRewind;

    public UnityEvent onMouseEnter { get; } = new();
    public UnityEvent onMouseExit { get; } = new();
    public UnityEvent onMouseDown { get; } = new();

    private void Awake()
    {
        ICreationObservable<Rewindable>.NotifyCreated(this);
        
        _startPfx ??= Resources.Load<GameObject>("ParticleEffects/RewindStart");
        _endPfx ??= Resources.Load<GameObject>("ParticleEffects/RewindComplete");
        _activePfx ??= Resources.Load<GameObject>("ParticleEffects/RewindActive");
        
        startPfx = Instantiate(_startPfx, transform, false).GetComponent<ParticleSystem>();
        endPfx = Instantiate(_endPfx, transform, false).GetComponent<ParticleSystem>();
        activePfx = Instantiate(_activePfx, transform, false).GetComponent<ParticleSystem>();
    }

    private void OnDestroy()
    {
        CancelRewind();
        ICreationObservable<Rewindable>.NotifyDestroyed(this);
    }
    
    private void Start()
    { 
        snapshots.Add(new TransformSnapshot(transform, lastUpdate));
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer) Destroy(this);
    }
    
    private void OnMouseEnter()
    {
        onMouseEnter.Invoke();
    }

    private void OnMouseExit() => onMouseExit.Invoke();

    private void OnMouseDown() => onMouseDown.Invoke();

    private void FixedUpdate()
    {
        if (isRewinding) return;

        var current = new TransformSnapshot(transform, lastUpdate);
        if (snapshots.Count == 0 || (snapshots.Count > 0 && HasMoved(snapshots.Last(), current) && lastUpdate > 0.01f))
        {
            snapshots.Add(new TransformSnapshot(transform, lastUpdate));
            lastUpdate = 0;
        }
        
        lastUpdate += Time.fixedDeltaTime;
    }

    private static bool HasMoved(TransformSnapshot last, TransformSnapshot current)
    {
        var posDelta = (last.Position - current.Position).magnitude;
        if (posDelta > 0.01f) return true;
        
        var oriDelta = (last.Rotation.eulerAngles - current.Rotation.eulerAngles).magnitude;
        if (oriDelta > 0.01f) return true;
        
        var scaDelta = (last.Scale - current.Scale).magnitude;
        if (scaDelta > 0.01f) return true;
        
        return false;
    }

    public IEnumerator Rewind()
    {
        if (snapshots.Count == 0) yield break;
        isRewinding = true;

        startPfx.Play();
        
        var snapshotsToUse = new List<TransformSnapshot>(snapshots);
        var snapshotsToRemove = new List<TransformSnapshot>();
        snapshotsToUse.Reverse();
        var lastSnapshot = new TransformSnapshot(transform, 0);
        activePfx.Play();
        do
        {
            var snapshot = snapshotsToUse.First();
            snapshotsToUse.RemoveAt(0);
            snapshotsToRemove.Add(snapshot);
            if (snapshot == null) continue;
            
            var time = Mathf.Min(Mathf.Max(snapshot.TimeDelta, 0.01f),0.1f);
            if (!HasMoved(snapshot, lastSnapshot)) continue;
            
            transform.DOMove(snapshot.Position, time).SetEase(Ease.Linear);
            transform.DORotateQuaternion(snapshot.Rotation, time).SetEase(Ease.Linear);
            transform.DOScale(snapshot.Scale, time).SetEase(Ease.Linear);
            
            yield return new WaitForSeconds(time);
            lastSnapshot = snapshot;
        } while (snapshotsToUse.Count > 0 && !forceCancelRewind);
        activePfx.Stop();
        endPfx.Play();
        
        forceCancelRewind = false;
        snapshots.RemoveAll(snapshot => snapshotsToRemove.Find(s => s.Equals(snapshot)) != null);
        isRewinding = false;
    }

    public void CancelRewind() => forceCancelRewind = true;
    
    public List<TransformSnapshot> GetSnapshots() => snapshots;
}