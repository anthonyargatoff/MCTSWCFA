

using UnityEngine;

public class EyeTracker: MonoBehaviour
{
    private Transform player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    private void Update()
    {
        if (player)
        {
            var angleBetween = Mathf.Atan2(player.transform.position.y - transform.position.y, player.transform.position.x - transform.position.x) * 180 / Mathf.PI;
            
            var zAngle = Mathf.Max(angleBetween, -45f);
            var xAngle = Mathf.Abs(angleBetween) - 90f;
            if (xAngle < 0) xAngle = 0;
            var yAngle = Mathf.Max(-angleBetween, 0f);
            yAngle = Mathf.Clamp(yAngle, 45f, 90f);
            transform.rotation = Quaternion.Euler(xAngle, yAngle, zAngle);
            return;
        }
        transform.rotation = Quaternion.Euler(0, 90f, 0);
    }
}