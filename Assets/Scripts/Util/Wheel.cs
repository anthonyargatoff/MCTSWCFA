using UnityEngine;

public class Wheel : MonoBehaviour
{
  [SerializeField] private float rotationSpeed;

  private Rewindable rewindableScript;
  private void Awake()
  {
    rewindableScript = GetComponent<Rewindable>();
  }
  
  // Update is called once per frame
  void Update()
  {
    if (rewindableScript && rewindableScript.isRewinding) return;
    transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
  }
}
