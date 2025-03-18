
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRewindController: MonoBehaviour, ICreationObserver<Rewindable>
{
    [SerializeField] private float rewindRange = 100f;
    [SerializeField] private LineRenderer sceneLineRenderer;
    public bool IsRewinding { get; private set; }
    public bool RewindActive { get; private set; }

    private Rewindable rewoundObject;
    private Rewindable focusRewindable;
    
    private void Awake()
    {
        ICreationObservable<Rewindable>.Subscribe(this);
    }

    private void OnDestroy()
    {
        ICreationObservable<Rewindable>.Unsubscribe(this);
    }

    private void OnToggleRewind()
    {
        if (RewindActive)
        {
            rewoundObject.CancelRewind();
        }
        else
        {
            IsRewinding = !IsRewinding;
            Time.timeScale = IsRewinding ? 0 : 1;   
        }
    }
    
    private void Update()
    {
       HighlightFocusRewindable();
    }

    private void HighlightFocusRewindable()
    {
        if (focusRewindable == null || !IsRewinding || RewindActive)
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
        if (RewindActive) yield break;
        rewoundObject = rewindable;
        OnToggleRewind();
        RewindActive = true;

        yield return rewindable.Rewind();
        
        RewindActive = false;
        rewoundObject = null;
    }

    private void SetFocusRewindable(Rewindable rewindable)
    {
        if ((transform.position - rewindable.transform.position).magnitude > rewindRange) return;
        focusRewindable = rewindable;
    }

    private void RemoveFocusRewindable(Rewindable rewindable)
    {
        focusRewindable = focusRewindable != null && focusRewindable.Equals(rewindable) ? null : focusRewindable;
    }

    public void OnObservableCreated(Rewindable obj)
    {
        obj.onMouseDown.AddListener(() => StartCoroutine(OnRewindableSelected(obj)));
        obj.onMouseEnter.AddListener(() => SetFocusRewindable(obj));
        obj.onMouseExit.AddListener(() => RemoveFocusRewindable(obj));
    }

    public void OnObservableDestroyed(Rewindable obj)
    {
    }
}