using UnityEngine;

public class Collectible : MonoBehaviour
{
    public void Collect()
    {
        Destroy(gameObject);
    }
}
