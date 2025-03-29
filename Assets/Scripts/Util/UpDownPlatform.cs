using System.Collections;
using DG.Tweening;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using UnityEngine;

public class UpDownPlatform : MonoBehaviour
{
  [SerializeField] private float platformSpeed;
  private Rigidbody2D platformRigidBody;
  private bool movingUp = true;

  private Rewindable rewindable;
  private float upperY = float.MinValue;
  private float lowerY = float.MaxValue;

  private void Start()
  {
    platformRigidBody = GetComponent<Rigidbody2D>();
    rewindable = GetComponent<Rewindable>();
    
    for (var i = 0; i < transform.parent.childCount; i++)
    {
      var c = transform.parent.GetChild(i);
      if (c.CompareTag("UpDownPlatformTrigger"))
      {
        upperY = Mathf.Max(c.position.y, upperY);
        lowerY = Mathf.Min(c.position.y, lowerY);
      }
    }
    
    StartCoroutine(MovePlatform());
  }
  
  private IEnumerator MovePlatform()
  {
    while (enabled && !this.IsDestroyed())
    {
      if (rewindable && rewindable.isRewinding)
      {
        yield return new WaitForSeconds(1f);
      }
      
      var dt = Mathf.Abs(upperY - lowerY) / platformSpeed;
      var t = 0f;
      var tw = transform.DOMoveY(movingUp ? upperY : lowerY, dt).SetEase(Ease.InOutSine).SetLink(gameObject);
      while (t < dt)
      {
        t += Time.deltaTime;
        if (rewindable && rewindable.isRewinding)
        {
          tw.Kill();
          break;
        }
        yield return new WaitForEndOfFrame();
      }
      movingUp = Mathf.Abs(transform.position.y - upperY) > Mathf.Abs(transform.position.y - lowerY);
      yield return new WaitForSeconds(1f); 
    }
  }
}
