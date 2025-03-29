
using UnityEngine;

public class MatchColliderToSprite: MonoBehaviour
{
    private SpriteRenderer sr;
    private BoxCollider2D cd;

    private Vector2 originalSize = Vector2.zero;
    private Vector2 prevSize = Vector2.zero;
    
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cd = GetComponent<BoxCollider2D>();

        originalSize = sr.size;
        prevSize = sr.size;
        AlignToSprite();
    }

    private void Update()
    {
        if (sr.drawMode.Equals(SpriteDrawMode.Tiled) && !prevSize.Equals(sr.size))
        {
            AlignToSprite();
            prevSize = sr.size;
        }
    }

    private void AlignToSprite()
    {
        const float rad = 2f;
        cd.edgeRadius = rad / 10f;
        var boxResize = Vector2.one * (Mathf.Pow(3,rad)/10);
        cd.size = prevSize - boxResize;
    }
}