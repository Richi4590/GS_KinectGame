using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeTrigger : MonoBehaviour
{
    public float ViewRadius
    {
        get => _viewRadius;
        set
        {
            if (_viewRadius != value)
            {
                _viewRadius = value;
                OnViewSettingsChanged();
            }
        }
    }

    public float ViewAngle
    {
        get => _viewAngle;
        set
        {
            if (_viewAngle != value)
            {
                _viewAngle = value;
                OnViewSettingsChanged();
            }
        }
    }

    public int Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution != value)
            {
                _resolution = value;
                OnViewSettingsChanged();
            }
        }
    }

    public int RayCount
    {
        get => _rayCount;
        set
        {
            if (_rayCount != value)
            {
                _rayCount = value;
                OnViewSettingsChanged();
            }
        }
    }


    [Header("Layer Masks")]
    public LayerMask targetMask;     // e.g., "Player"
    public LayerMask obstaclesMask;     // e.g., Everything else

    public bool drawRays = true;
    public bool drawMesh = true;


    [Header("Send Message Settings")]
    public bool AlsoSendMessages = false;
    public List<string> TagsToReactTo = new List<string>();
    public List<string> functionStrings = new List<string>();

    [Header("Debug")]
    public Color rayColor = new Color(1, 1, 0, 0.2f);
    public List<Transform> visibleTargets = new List<Transform>();

    [Header("FOV Settings")]
    [SerializeField] private float _viewRadius = 10;
    [SerializeField][Range(0, 360)] public float _viewAngle = 90f;
    [SerializeField][Min(1)] private int _rayCount = 50;
    [SerializeField][Min(1)] private int _resolution = 5;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh coneMesh;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    public UnityEvent onSight;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = drawMesh;
    }

    void Start()
    {
        GenerateConeMesh();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            OnViewSettingsChanged();
    }


    void FixedUpdate()
    {

        visibleTargets.Clear();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        lineRenderers.Clear();

        float angleStep = ViewAngle / (RayCount - 1);
        Quaternion startRot = Quaternion.Euler(0, -_viewAngle / 2f, 0);

        for (int i = 0; i < RayCount; i++)
        {
            float angle = angleStep * i;
            Vector3 dir = transform.rotation * startRot * Quaternion.Euler(0, angle, 0) * Vector3.forward;

            Ray ray = new Ray(transform.position, dir);
            Vector3 endPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, ViewRadius, targetMask | obstaclesMask))
            {
                endPoint = hit.point; 

                if (((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                {
                    visibleTargets.Add(hit.transform);
                    onSight.Invoke();

                    if (AlsoSendMessages)
                    {
                        if (Utilities.HasCustomTag(hit.collider.gameObject, TagsToReactTo))
                            foreach (string functionString in functionStrings)
                                hit.collider.gameObject.SendMessage(functionString, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            else
            {
                endPoint = transform.position + dir * ViewRadius;
            }

            if (drawRays)
            {
                Debug.DrawLine(transform.position, endPoint, Color.red);
                CreateRayLine(transform.position, endPoint);
            }

        }

    }

    private void OnViewSettingsChanged()
    {
        GenerateConeMesh();
    }

    void GenerateConeMesh()
    {
        coneMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero); // origin point

        float angleStep = ViewAngle / Resolution;
        for (int i = 0; i <= Resolution; i++)
        {
            float angle = -ViewAngle / 2f + angleStep * i;
            Vector3 localDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            Vector3 worldEnd = transform.position + (transform.rotation * localDir * ViewRadius);
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

        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    meshFilter = GetComponent<MeshFilter>();
                    meshFilter.mesh = coneMesh;
                }
            };
            return;
        }
        else
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
