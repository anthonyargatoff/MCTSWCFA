using UnityEngine;

public class Wheel : MonoBehaviour
{
  [SerializeField] private float rotationSpeed;

  // Update is called once per frame
  void Update()
  {
    transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
  }
}
