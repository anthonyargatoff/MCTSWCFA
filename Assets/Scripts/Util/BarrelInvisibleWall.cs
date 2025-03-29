
using UnityEngine;

public class BarrelInvisibleWall: MonoBehaviour
{
    private void Start()
    {
        var boxCollider = GetComponentInParent<BoxCollider2D>();
        var thisCollider = GetComponent<BoxCollider2D>();
        if (boxCollider && thisCollider)
        {
            thisCollider.size = boxCollider.size;
        }
    }        
}