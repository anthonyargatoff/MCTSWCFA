

using UnityEngine;

public class EyeTracker: MonoBehaviour
{
    private Transform player;
    private Camera mainCamera;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    private void Update()
    {
        if (player)
        {
            var xDiff = (player.position.x - transform.position.x) / mainCamera.orthographicSize * 45f;
            var yDiff = (player.position.y - transform.position.y) / mainCamera.orthographicSize * 45f;
            
            var yAngle = xDiff;
            var zAngle = yDiff;
            yAngle = Mathf.Clamp(-yAngle, -45f, 45f);
            zAngle = Mathf.Clamp(zAngle, -45f, 45f);
            transform.localRotation = Quaternion.Euler(0, yAngle, zAngle);
            return;
        }
        transform.rotation = Quaternion.identity;
    }
}