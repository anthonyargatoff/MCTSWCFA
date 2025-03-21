using UnityEngine;

public class WheelPlatform : MonoBehaviour
{
  private Quaternion initialRotation;
  void Start()
  {
    initialRotation = transform.rotation;
  }

  void Update()
  {
    transform.rotation = initialRotation;
  }
}
