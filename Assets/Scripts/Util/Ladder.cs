using UnityEngine;

public class Ladder : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    private GameObject ladderBottom;
    private GameObject ladderTop;
    private GameObject texture;
    private GameObject barrelTrigger;
    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        ladderTop = transform.Find("LadderTop")?.gameObject;
        ladderBottom = transform.Find("LadderBottom")?.gameObject;
        texture = transform.Find("Texture")?.gameObject;
        barrelTrigger = transform.Find("BarrelTrigger")?.gameObject;
        AdaptTexture();
    }

    private void AdaptTexture()
    {
        var height = ladderTop.transform.localPosition.y - ladderBottom.transform.localPosition.y;
        var sizeDelta = ladderBottom.transform.localScale.y;
        
        var spriteRenderer = texture.GetComponent<SpriteRenderer>();
        spriteRenderer.size = new Vector2(1, height);
        boxCollider.size = spriteRenderer.size;
        boxCollider.size += new Vector2(0, 0.5f);
        
        texture.transform.localPosition = new Vector3(0, sizeDelta, 0);
        boxCollider.offset = new Vector2(0, sizeDelta + 0.5f);
        
        if (barrelTrigger)
        {
            barrelTrigger.transform.localPosition = new Vector3(0, sizeDelta, 0);   
        }
    }
}
