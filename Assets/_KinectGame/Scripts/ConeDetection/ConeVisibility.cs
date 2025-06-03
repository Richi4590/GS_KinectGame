using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode]
public class ConeTrigger : MonoBehaviour
{
    [Header("FOV Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public int rayCount = 50;

    [Header("Layer Masks")]
    public LayerMask targetMask;     // e.g., "Player"
    public LayerMask obstaclesMask;     // e.g., Everything else

    public bool drawRays = true;
    public bool drawMesh = true;


    [Header("Debug")]
    public Color rayColor = new Color(1, 1, 0, 0.2f);
    public List<Transform> visibleTargets = new List<Transform>();

    public UnityEvent onSight;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh coneMesh;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    public int resolution = 5;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = drawMesh;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GenerateConeMesh();
    }


    void Update()
    {
        if (Application.isEditor)
            GenerateConeMesh();

        visibleTargets.Clear();

        if (Application.isPlaying)
        {

            // Clear previous LineRenderersS
            foreach (var lr in lineRenderers)
            {
                if (lr != null)
                    Destroy(lr.gameObject);
            }
            lineRenderers.Clear();
        }

        float angleStep = viewAngle / (rayCount - 1);
        Quaternion startRot = Quaternion.Euler(0, -viewAngle / 2f, 0);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = angleStep * i;
            Vector3 dir = transform.rotation * startRot * Quaternion.Euler(0, angle, 0) * Vector3.forward;

            Ray ray = new Ray(transform.position, dir);
            Vector3 endPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, viewRadius, targetMask | obstaclesMask))
            {
                endPoint = hit.point;

                if (((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                {
                    visibleTargets.Add(hit.transform);
                    onSight.Invoke();
                }
            }
            else
            {
                endPoint = transform.position + dir * viewRadius;
            }

            if (drawRays)
            {
                Debug.DrawLine(transform.position, endPoint, Color.red);
                CreateRayLine(transform.position, endPoint);
            }

        }

    }


    void GenerateConeMesh()
    {
        coneMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero); // origin point

        float angleStep = viewAngle / resolution;
        for (int i = 0; i <= resolution; i++)
        {
            float angle = -viewAngle / 2f + angleStep * i;
            Vector3 localDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            Vector3 worldEnd = transform.position + (transform.rotation * localDir * viewRadius);
            Vector3 localEnd = transform.InverseTransformPoint(worldEnd);
            vertices.Add(localEnd);
        }

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        coneMesh.SetVertices(vertices);
        coneMesh.SetTriangles(triangles, 0);
        coneMesh.RecalculateNormals();

        meshFilter.mesh = coneMesh;
    }

    void CreateRayLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("RayLine");
        go.transform.parent = this.transform;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = rayColor;
        lr.endColor = rayColor;
        lr.startWidth = 0.2f;
        lr.endWidth = 0.2f;
        lr.numCapVertices = 2;

        lineRenderers.Add(lr);
    }
}
