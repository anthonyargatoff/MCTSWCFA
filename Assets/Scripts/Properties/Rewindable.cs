using System;
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
        Scale = new Vector3(transform.localScale.x,transform.localScale.y,transform.localScale.z);
        TimeDelta = timeDelta;
    }
}

public class Rewindable: MonoBehaviour, ICreationObservable<Rewindable>
{
    private static GameObject _startPfx;
    private static GameObject _endPfx;
    private static GameObject _activePfx;

    private Tuple<ParticleSystem,ParticleSystem> startPfx;
    private Tuple<ParticleSystem,ParticleSystem> endPfx;
    private Tuple<ParticleSystem,ParticleSystem> activePfx;
    
    private readonly List<TransformSnapshot> snapshots = new();
    private readonly List<RewindableChild> children = new();
    private float lastUpdate;
    
    public bool isRewinding { get; private set; }

    private bool forceCancelRewind;
    public UnityEvent onMouseEnter { get; } = new();
    public UnityEvent onMouseExit { get; } = new();
    public UnityEvent onMouseDown { get; } = new();

    private Collider2D thisCollider;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        ICreationObservable<Rewindable>.NotifyCreated(this);
        
        _startPfx ??= Resources.Load<GameObject>("ParticleEffects/RewindStart");
        _endPfx ??= Resources.Load<GameObject>("ParticleEffects/RewindComplete");
        _activePfx ??= Resources.Load<GameObject>("ParticleEffects/RewindActive");
        
        startPfx = new Tuple<ParticleSystem, ParticleSystem>(
            Instantiate(_startPfx, transform, false).GetComponent<ParticleSystem>(), 
            _startPfx.GetComponent<ParticleSystem>()
        );
        endPfx = new Tuple<ParticleSystem, ParticleSystem>(
            Instantiate(_endPfx, transform, false).GetComponent<ParticleSystem>(), 
            _endPfx.GetComponent<ParticleSystem>()
        );
        activePfx = new Tuple<ParticleSystem, ParticleSystem>(
            Instantiate(_activePfx, transform, false).GetComponent<ParticleSystem>(), 
            _activePfx.GetComponent<ParticleSystem>()
        );
        
        thisCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        
        RecursiveChildParse(transform);
    }

    private void OnDestroy()
    {
        foreach (var child in children)
        {
            Destroy(child);
        }
        CancelRewind();
        ICreationObservable<Rewindable>.NotifyDestroyed(this);
    }
    
    private void Start()
    { 
        snapshots.Add(new TransformSnapshot(transform, lastUpdate));
    }
    
    private void OnMouseEnter() => onMouseEnter.Invoke();

    private void OnMouseExit() => onMouseExit.Invoke();

    private void OnMouseDown() => onMouseDown.Invoke();

    private void RecursiveChildParse(Transform t)
    {
        for (var i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.GetComponent<Collider2D>())
            {
                var rc = child.AddComponent<RewindableChild>();
            
                rc.onMouseEnter += () => onMouseEnter.Invoke();
                rc.onMouseExit += () => onMouseExit.Invoke();
                rc.onMouseDown += () => onMouseDown.Invoke();
                
                children.Add(rc);
            }
            RecursiveChildParse(child);
        }
    }

    private void Update()
    {
        if (thisCollider)
        {
            var mag = Mathf.Max(thisCollider.bounds.size.x, thisCollider.bounds.size.y);
            var particleScale = Vector3.one * mag;
            particleScale.z = 1;

            foreach (var tuple in new[] { startPfx, activePfx, endPfx })
            {
                var pfx = tuple.Item1;
                var basepfx = tuple.Item2;
                
                if (pfx.isPlaying) return;
                var shape = pfx.shape;
                shape.scale = particleScale;

                var main = pfx.main;
                main.startSpeed = UpdateMinMaxCurve(pfx.main.startSpeed, basepfx.main.startSpeed, mag);
                
                var emission = pfx.emission;
                if (emission.enabled)
                {
                    emission.rateOverTime = UpdateMinMaxCurve(pfx.emission.rateOverTime, basepfx.emission.rateOverTime, mag);
                    emission.rateOverDistance = UpdateMinMaxCurve(pfx.emission.rateOverDistance, basepfx.emission.rateOverDistance, mag);

                    for (var i = 0; i < emission.burstCount; i++)
                    {
                        var burst = emission.GetBurst(i);
                        var baseBurst = basepfx.emission.GetBurst(i);
                        
                        burst.count = UpdateMinMaxCurve(burst.count, baseBurst.count, mag);
                        emission.SetBurst(i, burst);
                    }
                }
                
                if (pfx.velocityOverLifetime.enabled)
                {
                    var velOverLife = pfx.velocityOverLifetime;
                    velOverLife.x = UpdateMinMaxCurve(pfx.velocityOverLifetime.x, basepfx.velocityOverLifetime.x, mag);
                    velOverLife.y = UpdateMinMaxCurve(pfx.velocityOverLifetime.y, basepfx.velocityOverLifetime.y, mag);
                    velOverLife.z = UpdateMinMaxCurve(pfx.velocityOverLifetime.z, basepfx.velocityOverLifetime.z, mag);
                    velOverLife.speedModifier = UpdateMinMaxCurve(pfx.velocityOverLifetime.speedModifier, basepfx.velocityOverLifetime.speedModifier, mag);
                }

                if (pfx.limitVelocityOverLifetime.enabled)
                {
                    var limitVelOverLife = pfx.limitVelocityOverLifetime;
                    if (pfx.limitVelocityOverLifetime.separateAxes)
                    {
                        limitVelOverLife.limitX = UpdateMinMaxCurve(pfx.limitVelocityOverLifetime.limitX, basepfx.limitVelocityOverLifetime.limitX, mag);
                        limitVelOverLife.limitY = UpdateMinMaxCurve(pfx.limitVelocityOverLifetime.limitY, basepfx.limitVelocityOverLifetime.limitY, mag);
                        limitVelOverLife.limitZ = UpdateMinMaxCurve(pfx.limitVelocityOverLifetime.limitZ, basepfx.limitVelocityOverLifetime.limitY, mag);
                    } else limitVelOverLife.limit = UpdateMinMaxCurve(pfx.limitVelocityOverLifetime.limit, basepfx.limitVelocityOverLifetime.limit, mag);
                    
                    limitVelOverLife.dampen = basepfx.limitVelocityOverLifetime.dampen * mag;
                    limitVelOverLife.drag = UpdateMinMaxCurve(pfx.limitVelocityOverLifetime.drag, basepfx.limitVelocityOverLifetime.drag, mag);
                }
            }
        }
        
        if (isRewinding) return;

        var current = new TransformSnapshot(transform, lastUpdate);
        if (snapshots.Count == 0 || (snapshots.Count > 0 && HasMoved(snapshots.Last(), current) && lastUpdate > 0.01f))
        {
            snapshots.Add(new TransformSnapshot(transform, lastUpdate));
            lastUpdate = 0;
            if (snapshots.Count > 500)
            {
                snapshots.RemoveAt(0);
            }
        }
        
        lastUpdate += Time.unscaledDeltaTime;
    }

    private static ParticleSystem.MinMaxCurve UpdateMinMaxCurve(ParticleSystem.MinMaxCurve value, ParticleSystem.MinMaxCurve baseValue, float mag)
    {
        switch (value.mode)
        {
            case ParticleSystemCurveMode.Curve:
                UpdateCurveKeyframes(value.curve.keys, baseValue.curve.keys, mag);
                break;
            case ParticleSystemCurveMode.TwoCurves:
                UpdateCurveKeyframes(value.curveMin.keys, baseValue.curveMin.keys, mag);
                UpdateCurveKeyframes(value.curveMax.keys, baseValue.curveMax.keys, mag);
                break;
            case ParticleSystemCurveMode.TwoConstants:
                value.constantMin = baseValue.constantMin * mag;
                value.constantMax = baseValue.constantMax * mag;
                break;
            default:
                value.constant = baseValue.constant * mag;
                break;
        }

        return value;
    }
    
    private static void UpdateCurveKeyframes(Keyframe[] keys, Keyframe[] basekeys, float mag)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            key.value = basekeys[i].value * mag;
            keys[i] = key;
        }
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

        startPfx.Item1.Play();

        var snapshotsToUse = new List<TransformSnapshot>(snapshots);
        var snapshotsToRemove = new List<TransformSnapshot>();
        snapshotsToUse.Reverse();
        var lastSnapshot = new TransformSnapshot(transform, 0);
        activePfx.Item1.Play();
        do
        {
            var snapshot = snapshotsToUse.First();
            snapshotsToUse.RemoveAt(0);
            snapshotsToRemove.Add(snapshot);
            if (snapshot == null) continue;

            var time = Mathf.Clamp(snapshot.TimeDelta, 0, 0.05f);
            if (!HasMoved(snapshot, lastSnapshot)) continue;

            transform.DOMove(snapshot.Position, time).SetEase(Ease.Linear).SetLink(gameObject);
            transform.DORotateQuaternion(snapshot.Rotation, time).SetEase(Ease.Linear).SetLink(gameObject);
            transform.DOScale(snapshot.Scale, time).SetEase(Ease.Linear).SetLink(gameObject);

            yield return new WaitForSeconds(time);
            lastSnapshot = snapshot;
        } while (snapshotsToUse.Count > 0 && !forceCancelRewind);

        if (activePfx.Item1 && endPfx.Item1)
        {
            activePfx.Item1.Stop();
            endPfx.Item1.Play();
        }

        if (!rb.IsDestroyed()) {
            var wasDynamic = rb.bodyType == RigidbodyType2D.Dynamic;
            if (wasDynamic)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;

                yield return new WaitForSeconds(0.5f);
                if (!rb.IsDestroyed())
                    rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        forceCancelRewind = false;
        snapshots.RemoveAll(snapshot => snapshotsToRemove.Find(s => s.Equals(snapshot)) != null);
        isRewinding = false;
    }

    public void CancelRewind() => forceCancelRewind = true;
    
    public List<TransformSnapshot> GetSnapshots() => snapshots;
}