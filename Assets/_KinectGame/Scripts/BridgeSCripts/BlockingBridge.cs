using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CompositeCollider2D), typeof(PolygonCollider2D))]
public class BlockingBridge : MonoBehaviour
{
    [Header("Blocking Area Settings")]
    public Vector2 areaSize = new Vector2(5, 2);
    public Color gizmoColor = new Color(1, 0, 0, 0.3f);

    private CompositeCollider2D compositeCollider;
    private PolygonCollider2D polygonCollider;

    [SerializeField] private TriggerEventsBlockingBridge triggerArea;

    private void Awake()
    {
        // Rigidbody2D is required for CompositeCollider2D (must be Static)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        polygonCollider = GetComponent<PolygonCollider2D>();

        // CompositeCollider2D to merge the base + cut-outs
        compositeCollider = GetComponent<CompositeCollider2D>();

        triggerArea.GetComponent<BoxCollider2D>().size = areaSize;
        SetBaseColliderShape();
    }

    private void SetBaseColliderShape()
    {
        // Convert the BoxCollider2D size into a PolygonCollider2D shape
        Vector2 halfSize = areaSize * 0.5f;
        Vector2[] boxPoints = new Vector2[]
        {
            new Vector2(-halfSize.x, -halfSize.y),
            new Vector2(halfSize.x, -halfSize.y),
            new Vector2(halfSize.x, halfSize.y),
            new Vector2(-halfSize.x, halfSize.y)
        };

        polygonCollider.SetPath(0, boxPoints);
    }

    public void UpdateBlockerCollider(HashSet<GameObject> bridgesInside)
    {
        // Remove old bridge colliders
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // If no bridges, just reset to base collider
        if (bridgesInside.Count == 0)
        {
            compositeCollider.GenerateGeometry();
            return;
        }

        // Add bridge colliders as "holes"
        foreach (GameObject bridge in bridgesInside)
        {
            PolygonCollider2D bridgeCollider = bridge.GetComponent<PolygonCollider2D>();
            if (bridgeCollider != null)
            {
                GameObject holeObject = new GameObject("BridgeHole");
                holeObject.transform.localScale = DepthObjectDetectorTopDown.Instance.generated2DMeshObjScale;
                holeObject.transform.SetParent(transform);
                holeObject.transform.position = bridge.transform.position;

                PolygonCollider2D holeCollider = holeObject.AddComponent<PolygonCollider2D>();
                holeCollider.pathCount = bridgeCollider.pathCount;
                holeCollider.isTrigger = true;
                holeCollider.compositeOperation = Collider2D.CompositeOperation.Difference;

                // Copy bridge shape into the new collider
                for (int i = 0; i < bridgeCollider.pathCount; i++)
                {
                    holeCollider.SetPath(i, bridgeCollider.GetPath(i));
                }
            }
        }

        // Force CompositeCollider2D to recalculate its geometry
        compositeCollider.GenerateGeometry();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }
}