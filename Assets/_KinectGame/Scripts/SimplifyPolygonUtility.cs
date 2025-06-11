using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SimplifyPolygonUtility
{
    public static Vector2[] SimplifyPolygon(Vector2[] points, float tolerance)
    {
        if (points.Length < 3) return points; // No need to simplify if less than 3 points
        List<Vector2> simplified = RamerDouglasPeucker(points, tolerance);
        return simplified.ToArray();
    }

    private static List<Vector2> RamerDouglasPeucker(Vector2[] points, float epsilon)
    {
        try
        {
            if (points.Length < 3) return new List<Vector2>(points);

            int firstIndex = 0;
            int lastIndex = points.Length - 1;
            List<int> pointIndexesToKeep = new List<int> { firstIndex, lastIndex };

            while (points[firstIndex] == points[lastIndex]) lastIndex--; // Avoid duplicates

            Reduce(points, firstIndex, lastIndex, epsilon, pointIndexesToKeep);
            pointIndexesToKeep.Sort();

            List<Vector2> result = new List<Vector2>();
            foreach (int index in pointIndexesToKeep)
            {
                result.Add(points[index]);
            }
            return result;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        return new List<Vector2>(points);
    }

    private static void Reduce(Vector2[] points, int firstIndex, int lastIndex, float epsilon, List<int> pointIndexesToKeep)
    {

        float maxDistance = 0;
        int index = firstIndex;

        for (int i = firstIndex + 1; i < lastIndex; i++)
        {
            float distance = PerpendicularDistance(points[firstIndex], points[lastIndex], points[i]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }

        if (maxDistance > epsilon)
        {
            pointIndexesToKeep.Add(index);
            Reduce(points, firstIndex, index, epsilon, pointIndexesToKeep);
            Reduce(points, index, lastIndex, epsilon, pointIndexesToKeep);
        }
    }

    private static float PerpendicularDistance(Vector2 pointA, Vector2 pointB, Vector2 point)
    {
        float area = Mathf.Abs((pointA.x * (pointB.y - point.y) + pointB.x * (point.y - pointA.y) + point.x * (pointA.y - pointB.y)) * 0.5f);
        float baseLength = (pointB - pointA).magnitude;
        return (area * 2f) / baseLength;
    }
}
