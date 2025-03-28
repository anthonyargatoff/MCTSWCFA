
using UnityEngine;

public class QuadraticBezier
{
    public static Vector3[] GenerateBezierCurvePath(Vector3 start, Vector3 end, Vector3 control, int nPoints)
    {
        var positions = new Vector3[nPoints];
        for (var i = 0; i < nPoints; i++)
        {
            var t = i / (float) nPoints;
            positions[i] = (Mathf.Pow(1-t,2) * start) + (2f * (1-t) * t * control) + (Mathf.Pow(t,2) * end);
        }
        return positions;
    }        
}