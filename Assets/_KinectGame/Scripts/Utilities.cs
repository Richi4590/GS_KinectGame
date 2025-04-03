using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Utilities : MonoBehaviour
{
    public static bool IsInSelectedLayers(LayerMask layersToCheck, GameObject obj)
    {
        // Get the layer of the current object
        int objectLayer = obj.layer;

        // Check if the object's layer is in the selected layers mask
        return (layersToCheck.value & (1 << objectLayer)) != 0;
    }

    public static bool IsColliderInside(Collider2D colliderA, Collider2D colliderB, float threshold = 0.5f)
    {
        if (colliderA == null || colliderB == null) return false;

        float overlapArea = GetColliderOverlapArea(colliderA, colliderB);
        float totalArea = GetColliderArea(colliderA);

        // Calculate overlap percentage
        float overlapPercentage = overlapArea / totalArea;

        return overlapPercentage >= threshold;
    }


    public static float GetColliderOverlapArea(Collider2D colliderA, Collider2D colliderB)
    {
        if (colliderA is BoxCollider2D boxA && colliderB is BoxCollider2D boxB)
        {
            return GetBoxColliderOverlapArea(boxA, boxB);
        }
        else if (colliderA is CircleCollider2D circleA && colliderB is CircleCollider2D circleB)
        {
            return GetCircleColliderOverlapArea(circleA, circleB);
        }
        else if (colliderA is PolygonCollider2D polyA && colliderB is PolygonCollider2D polyB)
        {
            return GetPolygonColliderOverlapArea(polyA, polyB);
        }
        else if (colliderA is CapsuleCollider2D capsuleA && colliderB is CapsuleCollider2D capsuleB)
        {
            return GetCapsuleColliderOverlapArea(capsuleA, capsuleB);
        }
        return 0f;
    }

    private static float GetBoxColliderOverlapArea(BoxCollider2D boxA, BoxCollider2D boxB)
    {
        Bounds boundsA = boxA.bounds;
        Bounds boundsB = boxB.bounds;

        // Find overlapping width
        float overlapWidth = Mathf.Max(0, Mathf.Min(boundsA.max.x, boundsB.max.x) - Mathf.Max(boundsA.min.x, boundsB.min.x));

        // Find overlapping height
        float overlapHeight = Mathf.Max(0, Mathf.Min(boundsA.max.y, boundsB.max.y) - Mathf.Max(boundsA.min.y, boundsB.min.y));

        // Overlap area is width * height
        return overlapWidth * overlapHeight / 100;
    }

    private static float GetCircleColliderOverlapArea(CircleCollider2D circleA, CircleCollider2D circleB)
    {
        // Calculate the distance between the centers
        float distance = Vector2.Distance(circleA.transform.position, circleB.transform.position);

        if (distance >= (circleA.radius + circleB.radius))
        {
            // No overlap if distance is greater than the sum of the radii
            return 0f;
        }
        else if (distance <= Mathf.Abs(circleA.radius - circleB.radius))
        {
            // One circle is fully inside the other
            float smallerRadius = Mathf.Min(circleA.radius, circleB.radius);
            return Mathf.PI * Mathf.Pow(smallerRadius, 2);
        }
        else
        {
            // Partial overlap - Calculate using geometry (circle-circle overlap formula)
            float r1 = circleA.radius;
            float r2 = circleB.radius;

            // Using circle-circle intersection formula
            float d = distance;
            float part1 = r1 * r1 * Mathf.Acos((d * d + r1 * r1 - r2 * r2) / (2 * d * r1));
            float part2 = r2 * r2 * Mathf.Acos((d * d + r2 * r2 - r1 * r1) / (2 * d * r2));
            float part3 = Mathf.Sqrt((-d + r1 + r2) * (d + r1 - r2) * (d - r1 + r2) * (d + r1 + r2)) / 2;

            return part1 + part2 - part3;
        }
    }

    private static float GetPolygonColliderOverlapArea(PolygonCollider2D polyA, PolygonCollider2D polyB)
    {
        // Polygon collision detection involves geometric algorithms.
        // One way to approach this is by using a polygon clipping library or custom code.

        // For simplicity, let's assume we use the "Shoelace" method to calculate areas and find intersections.
        // But for now, we'll return 0 as a placeholder since PolygonCollider2D intersection requires complex logic.
        return 0f;
    }

    private static float GetCapsuleColliderOverlapArea(CapsuleCollider2D capsuleA, CapsuleCollider2D capsuleB)
    {
        // Get radius and height for both capsules
        float radiusA = capsuleA.size.x / 2f; // Radius is half of width
        float heightA = capsuleA.size.y - 2 * radiusA; // Subtract hemispheres height from total height

        float radiusB = capsuleB.size.x / 2f; // Radius is half of width
        float heightB = capsuleB.size.y - 2 * radiusB; // Subtract hemispheres height from total height

        // Calculate the distance between the centers of the capsules
        float distance = Vector2.Distance(capsuleA.transform.position, capsuleB.transform.position);

        // If capsules are too far apart, there is no overlap
        if (distance >= (heightA + heightB + radiusA + radiusB))
        {
            return 0f;
        }

        // Cylinder-Cylinder Overlap: Calculate if the cylindrical portions of the capsules intersect
        float cylinderOverlap = 0f;
        if (distance < (heightA + heightB))
        {
            cylinderOverlap = Mathf.Max(0f, Mathf.Min(heightA, heightB) - Mathf.Abs(distance));
        }

        // Hemisphere-Hemisphere Overlap: Calculate if the hemispheres of the two capsules overlap
        float hemisphereOverlap = 0f;
        if (distance < (radiusA + radiusB))
        {
            hemisphereOverlap = Mathf.PI * Mathf.Pow(Mathf.Min(radiusA, radiusB), 2);
        }

        // Combine cylinder and hemisphere overlap (note: this is simplified, doesn't consider full geometry)
        return cylinderOverlap + hemisphereOverlap;
    }

    public static float GetColliderArea(Collider2D collider)
    {
        if (collider is BoxCollider2D)
        {
            BoxCollider2D castedCollider = (BoxCollider2D)collider;

            return GetBoxColliderArea(castedCollider);
        }
        else if (collider is CircleCollider2D circleA)
        {
            CircleCollider2D castedCollider = (CircleCollider2D)collider;

            return GetCircleColliderArea(castedCollider);
        }
        else if (collider is PolygonCollider2D polyA)
        {
            PolygonCollider2D castedCollider = (PolygonCollider2D)collider;

            return GetPolygonColliderArea(castedCollider);
        }
        else if (collider is CapsuleCollider2D capsuleA)
        {
            CapsuleCollider2D castedCollider = (CapsuleCollider2D)collider;

            return GetCapsuleColliderArea(castedCollider);
        }
        return 0f;
    }

    // BoxCollider2D area: width * height
    private static float GetBoxColliderArea(BoxCollider2D box)
    {
        return box.size.x * box.size.y;
    }

    // CircleCollider2D area: π * radius^2
    private static float GetCircleColliderArea(CircleCollider2D circle)
    {
        return Mathf.PI * Mathf.Pow(circle.radius, 2);
    }

    // PolygonCollider2D area using Shoelace Theorem
    private static float GetPolygonColliderArea(PolygonCollider2D polygon)
    {
        Vector2[] points = polygon.points;
        float area = 0f;
        int j = points.Length - 1;

        for (int i = 0; i < points.Length; i++)
        {
            area += (points[j].x + points[i].x) * (points[j].y - points[i].y);
            j = i; // current vertex becomes previous one
        }

        return Mathf.Abs(area) / 2f;
    }

    // CapsuleCollider2D area: 2 * π * radius * height + 2 * π * radius^2 (cylinder + hemispheres)
    private static float GetCapsuleColliderArea(CapsuleCollider2D capsule)
    {
        float radius = capsule.size.x / 2f; // Radius is half the width
        float height = capsule.size.y - 2 * radius; // Subtract hemispheres' height from total height

        // Capsule area = cylindrical area + 2 hemispherical areas
        return (2 * Mathf.PI * radius * height) + (2 * Mathf.PI * Mathf.Pow(radius, 2)); // Cylinder + hemispheres
    }
}
