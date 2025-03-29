using UnityEngine;

public class WheelPlatform : MonoBehaviour
{
  private Quaternion initialRotation;
  
  private void Start()
  {
    initialRotation = transform.rotation;
  }

  private void Update()
  {
    transform.rotation = initialRotation;
  }
}
