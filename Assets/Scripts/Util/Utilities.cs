
using System.Collections;
using UnityEngine;

public class Utilities: MonoBehaviour
{
    public static IEnumerator DrawDebugRay(Vector3 position, Vector3 direction, float range)
    {
        var go = new GameObject();

        var li = go.AddComponent<LineRenderer>();
        li.useWorldSpace = true;
        li.startWidth = 0.01f;
        li.endWidth = 0.01f;
        
        var p1 = position + direction * range;
        li.SetPositions(new [] { position, p1 });

        yield return new WaitForSeconds(3f);
        Destroy(go);
    }
}