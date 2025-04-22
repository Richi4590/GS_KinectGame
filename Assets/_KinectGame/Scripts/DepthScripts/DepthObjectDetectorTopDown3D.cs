using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;
using Windows.Kinect;
using Rect = UnityEngine.Rect;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using AYellowpaper.SerializedCollections;
using System;
using ZXing;
using OpenCvSharp.Util;

public class DepthObjectDetectorTopDown3D : MonoBehaviour
{
    public static DepthObjectDetectorTopDown3D Instance;

    public GameObject DepthSourceManager;
    public GameObject TopDownObjectPrefab3D;
    public SerializedDictionaryStringGameObject TopDownObjectsPrefabs3D;
    public GameObject DebugObjectCenterPrefab;
    public MeshRenderer depthTextureVisualDestinationMesh;
    public MeshRenderer QRCodeVisualizerMesh;
    public ObjectCreationMode currentMode = ObjectCreationMode.ReuseObjectAndUpdate;
    [Min(0)] public int MinDepth = 500;
    [Min(0)] public int MaxDepth = 1000;
    [Min(0)] public int minBlobSize = 10;
    [Min(0)] public int maxBlobSize = 600;
    [Min(0)] public int dilationIterations = 2;
    [Min(0)] public int erosionIterations = 2;
    [Range(0, 100)] public int borderThickness = 0;

    public Vector3 generated2DMeshObjScale = Vector3.one;
    [Min(0)] public float simplificationTolerance = 0.02f;

    public LayerMask layersToIgnoreMeshUpdates;  
    private Vector3 generatedMeshRotationOffset = Vector3.zero;

    public bool flipSpriteX = false;
    public bool flipSpriteY = false;

    public bool flipDepthMapX = false;
    public bool flipDepthMapY = false;

    public float maxAllowedMovementRadius = 1f;
    public float collisionWallHeight = 5f;
    public bool visualizeWithLines = true;
    public float lineWidth = 0.1f;
    public int framesToWait = 10;

    private KinectSensor _Sensor;
    private DepthSourceManager _DepthManager;
    private int _DepthWidth = 512;
    private int _DepthHeight = 424;
    private Texture2D _BinaryMaskTexture;
    private List<Rect> objectBounds;
    private List<GameObject> _SpawnedObjects = new List<GameObject>();
    private Dictionary<GameObject, Vector2> trackedObjects = new Dictionary<GameObject, Vector2>();
    private ObjectCreationMode previousMode;
    private CoordinateMapper _CoordinateMapper;

    private Camera _MainCamera;
    private Mat _BinaryImage;
    private Mat _Labels;
    private Mat _Stats;
    private Mat _Centroids;
    private Vector3[] worldPoints3D;

    private bool DEBUG_FloodFill_Mask = false;

    private WebCamTexture _WebCamTexture;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null) _Sensor.Open();

        _CoordinateMapper = _Sensor.CoordinateMapper;

        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
        _BinaryMaskTexture = new Texture2D(_DepthWidth, _DepthHeight);
        _MainCamera = Camera.main;

        _WebCamTexture = new WebCamTexture();
        _WebCamTexture.Play();

        if (TopDownObjectsPrefabs3D == null)
            TopDownObjectsPrefabs3D = GetComponent<SerializedDictionaryStringGameObject>();

        // Allocate OpenCV Mats only once
        _BinaryImage = new Mat(_DepthHeight, _DepthWidth, MatType.CV_8UC1);
        _Labels = new Mat();
        _Stats = new Mat();
        _Centroids = new Mat();
        objectBounds = new List<Rect>();

        previousMode = currentMode;
        StartCoroutine(ExecuteEveryNFrames());
        
    }

    void Update()
    {

    }

    // Coroutine that will execute every N frames
    IEnumerator ExecuteEveryNFrames()
    {
        while (true)
        {
            // Wait for N frames
            yield return new WaitForSeconds(1f / ((int)(1.0f / Time.smoothDeltaTime)) * framesToWait);
            ProcessDepthData(_DepthManager.GetData());
            //InstantiateObjectsFromBounds(objectBounds);

            if (previousMode != currentMode)
            {
                ClearSpawnedObjects();
                previousMode = currentMode;
            }

            if (currentMode == ObjectCreationMode.InstantiateObjectAndDestroyPerFrame)
            {
                InstantiateAndDestroyObjectsWithMesh();
            }
            else
            {
                InstantiateOrUpdateObjectsWithMesh();
            }

        }
    }

    void ProcessDepthDataOld(ushort[] depthData)
    {
        if (depthData == null) return;

        // Convert depth data to OpenCV Mat
        Mat depthMat = new Mat(_DepthHeight, _DepthWidth, MatType.CV_16UC1, depthData);
        Cv2.InRange(depthMat, MinDepth, MaxDepth, _BinaryImage);

        // Remove edge-connected components
        RemoveEdgeArtifacts(ref _BinaryImage);

        if (DEBUG_FloodFill_Mask)
            return;

        // Connected components analysis
        int numComponents = Cv2.ConnectedComponentsWithStats(_BinaryImage, _Labels, _Stats, _Centroids);
        objectBounds = new List<Rect>();

        for (int i = 1; i < numComponents; i++)  // Ignore background
        {
            int area = _Stats.At<int>(i, 4);
            if (minBlobSize <= area && area <= maxBlobSize)
            {
                int x = _Stats.At<int>(i, 0);
                int y = _Stats.At<int>(i, 1);
                int width = _Stats.At<int>(i, 2);
                int height = _Stats.At<int>(i, 3);
                objectBounds.Add(new Rect(x, y, width, height));
            }
        }

        ApplyMaskTexture();
    }

    void ProcessDepthData(ushort[] depthData)
    {
        if (depthData == null) return;
        // Convert depth data to OpenCV Mat
        Mat depthMat = new Mat(_DepthHeight, _DepthWidth, MatType.CV_16UC1, depthData);

        if (flipDepthMapX)
            Cv2.Flip(depthMat, depthMat, FlipMode.X);

        if (flipDepthMapY)
            Cv2.Flip(depthMat, depthMat, FlipMode.Y);

        Cv2.InRange(depthMat, MinDepth, MaxDepth, _BinaryImage);

        // Remove edge-connected components
        RemoveEdgeArtifacts(ref _BinaryImage);

        ApplyMaskTexture();
    }

    void RemoveEdgeArtifacts(ref Mat binaryImage)
    {
        // Ensure binary image is in the correct format
        if (binaryImage.Type() != MatType.CV_8UC1)
            binaryImage.ConvertTo(binaryImage, MatType.CV_8UC1);

        // Structuring element (kernel) for erosion and dilation
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));

        // Erode first to remove noise
        Cv2.Erode(binaryImage, binaryImage, kernel, iterations: erosionIterations);

        // Dilate back to restore object shapes
        Cv2.Dilate(binaryImage, binaryImage, kernel, iterations: dilationIterations);

        Mat modifiedMask = binaryImage.Clone();

        byte colorValue = 255; // White border color

        // Set top and bottom borders (horizontal)
        for (int i = 0; i < modifiedMask.Cols; i++)
        {
            for (int j = 0; j < borderThickness; j++)
            {
                modifiedMask.Set<byte>(j, i, colorValue); // Top border
                modifiedMask.Set<byte>(modifiedMask.Rows - 1 - j, i, colorValue); // Bottom border
            }
        }

        // Set left and right borders (vertical)
        for (int i = 0; i < modifiedMask.Rows; i++)
        {
            for (int j = 0; j < borderThickness; j++)
            {
                modifiedMask.Set<byte>(i, j, colorValue); // Left border
                modifiedMask.Set<byte>(i, modifiedMask.Cols - 1 - j, colorValue); // Right border
            }
        }

        if (DEBUG_FloodFill_Mask)
        {
            Texture2D texture = new Texture2D(modifiedMask.Cols, modifiedMask.Rows);

            // Convert the binary mask to a color (black and white)
            for (int y = 0; y < modifiedMask.Rows; y++)
            {
                for (int x = 0; x < modifiedMask.Cols; x++)
                {
                    byte pixelValue = modifiedMask.At<byte>(y, x);  // Get the pixel value (0 or 255)
                    Color color = pixelValue == 255 ? Color.red : Color.black;
                    texture.SetPixel(x, y, color);  // Set pixel to black or white
                }
            }

            // Apply changes to the texture
            texture.Apply();
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            return;
        }


        Cv2.FloodFill(modifiedMask, new Point(0, 0), Scalar.Black);
        
        if (!DEBUG_FloodFill_Mask)
            // Crop the image back to original size (remove the added border)
            binaryImage = modifiedMask;
    }

    void ApplyMaskTexture()
    {
        // Convert OpenCV Mat to Unity Texture
        byte[] pixels = new byte[_DepthWidth * _DepthHeight];
        _BinaryImage.GetArray(0, 0, pixels);

        for (int i = 0; i < pixels.Length; i++)
        {
            _BinaryMaskTexture.SetPixel(i % _DepthWidth, i / _DepthWidth, pixels[i] > 0 ? Color.white : Color.black);
        }

        _BinaryMaskTexture.Apply();
        depthTextureVisualDestinationMesh.material.mainTexture = _BinaryMaskTexture;
    }

    private void InstantiateOrUpdateObjectsWithMesh()
    {
        HierarchyIndex[] hierarchy;
        Point[][] contours;
        Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        HashSet<GameObject> updatedObjects = new HashSet<GameObject>();
        objectBounds.Clear();

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (minBlobSize <= area && area <= maxBlobSize)
                continue;

            // Convert to simplified 2D world contour
            List<Vector2> contour2D = new List<Vector2>();
            for (int i = 0; i < contour.Length; i++)
            {
                Vector3 pos = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
                contour2D.Add(new Vector2(pos.x, pos.z));
            }

            Vector2[] simplified = SimplifyPolygon(contour2D.ToArray(), simplificationTolerance);

            if (simplified.Length < 3)
                return;

            // Create 3D positions on XZ plane
            worldPoints3D = new Vector3[simplified.Length];

            for (int i = 0; i < simplified.Length; i++)
            {
                worldPoints3D[i] = new Vector3(simplified[i].x, depthTextureVisualDestinationMesh.transform.position.y, simplified[i].y);
            }

            // Compute center
            Vector3 minBounds = worldPoints3D[0];
            Vector3 maxBounds = worldPoints3D[0];
            foreach (var point in worldPoints3D)
            {
                minBounds = Vector3.Min(minBounds, point);
                maxBounds = Vector3.Max(maxBounds, point);
            }

            Vector3 size3D = maxBounds - minBounds;
            Vector3 center3D = minBounds + size3D / 2f;
            objectBounds.Add(new Rect(minBounds.x, minBounds.z, size3D.x, size3D.z));

            // Match to existing object
            GameObject closestObject = null;
            float minDistance = float.MaxValue;
            Vector2 newCenter2D = new Vector2(center3D.x, center3D.z);

            foreach (var kvp in trackedObjects)
            {
                float distance = Vector2.Distance(kvp.Value, newCenter2D);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestObject = kvp.Key;
                }
            }

            GameObject obj = null;
            if (closestObject != null && minDistance <= maxAllowedMovementRadius)
            {
                GameObject newlyScannedPrefab = GetGameObjectPrefabFromCameraContour(contour, center3D);

                if (closestObject.GetComponent<CustomTag>().tag != newlyScannedPrefab.GetComponent<CustomTag>().tag) //replace old object with the actual correct one
                {
                    trackedObjects.Remove(closestObject);
                    _SpawnedObjects.Remove(closestObject);
                    Destroy(closestObject);

                    closestObject = Instantiate(
                       newlyScannedPrefab,
                       center3D,
                       Quaternion.identity,
                       depthTextureVisualDestinationMesh.transform
                    );
                }

                // Reuse object
                obj = closestObject;
                obj.transform.position = center3D;
                trackedObjects[obj] = newCenter2D;

                if (!_SpawnedObjects.Contains(obj))
                    _SpawnedObjects.Add(obj);
            }
            else
            {
                obj = InstantiateGameObjectFromCameraContour(contour, center3D);

                if(obj == null)
                    return;

                trackedObjects[obj] = newCenter2D;
                _SpawnedObjects.Add(obj);
            }

            if (obj != null)
            {
                GameObject childPlaneObj = obj.transform.GetChild(0).gameObject;
                updatedObjects.Add(obj);

                Vector3 localCenter = depthTextureVisualDestinationMesh.transform.InverseTransformPoint(center3D);
                VisualizeWorldPoints3D(obj, worldPoints3D);

                Mesh planeMesh = TriangulateFull(worldPoints3D, localCenter, collisionWallHeight);
                Mesh wallMesh = ExtrudePolygon(worldPoints3D, localCenter, collisionWallHeight);

                if (wallMesh != null)
                {
                    if (!childPlaneObj.TryGetComponent<MeshFilter>(out var meshFilter))
                        meshFilter = childPlaneObj.AddComponent<MeshFilter>();
                    meshFilter.mesh = planeMesh;

                    if (!childPlaneObj.TryGetComponent<MeshCollider>(out var childMeshCollider))
                        childMeshCollider = obj.AddComponent<MeshCollider>();
                    childMeshCollider.sharedMesh = planeMesh;

                    //------------

                    if (!obj.TryGetComponent<MeshRenderer>(out var meshRenderer))
                        meshRenderer = obj.AddComponent<MeshRenderer>();
                    meshRenderer.material = TopDownObjectPrefab3D.GetComponent<MeshRenderer>().sharedMaterial;

                    // Optional: Enable mesh collider
                    if (!obj.TryGetComponent<MeshCollider>(out var meshCollider))
                        meshCollider = obj.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = wallMesh;
                }
                else
                {
                    Debug.LogWarning("Failed to create mesh for object with contour.");
                }
            }

            // Remove objects not updated
            List<GameObject> toRemove = new List<GameObject>();
            foreach (var o in trackedObjects.Keys)
            {
                if (!updatedObjects.Contains(o))
                {
                    toRemove.Add(o);
                }
            }

            foreach (var o in toRemove)
            {
                trackedObjects.Remove(o);
                _SpawnedObjects.Remove(o);
                Destroy(o);
            }
        }
    }

    public static Mat Sharpen(Mat input)
    {
        Mat sharpened = new Mat();
        float[] kernelData = {
         0, -1,  0,
        -1,  5, -1,
         0, -1,  0
    };
        var kernel = new Mat(3, 3, MatType.CV_32FC1, kernelData);
        Cv2.Filter2D(input, sharpened, input.Depth(), kernel);
        return sharpened;
    }

    private GameObject InstantiateGameObjectFromCameraContour(Point[] contour, Vector3 center3D)
    {
        OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contour);

        Texture2D cameraTex = ColorSourceManager.Instance.GetColorTexture();

        if (cameraTex == null)
        {
            Debug.LogWarning("Failed to read camera texture! No ColorSourceManager in Scene?");
            return null;
        }

        Mat colorMat = Texture2DToMat(cameraTex);

        Texture2D camSegment = GetColorSegmentFromDepthContour(
            _DepthManager.GetData(), boundingRect, _CoordinateMapper, colorMat,
            _DepthWidth, _DepthHeight, ColorSourceManager.Instance.GetColorTexture().width, ColorSourceManager.Instance.GetColorTexture().height
        );

        if (camSegment != null)
        {
            QRCodeVisualizerMesh.material.mainTexture = camSegment;

            // Instantiate object at calculated 3D center
            return InstantiateFromQRCode(camSegment, center3D, Quaternion.identity, depthTextureVisualDestinationMesh.transform);
        }

        return null;
    }

    private GameObject GetGameObjectPrefabFromCameraContour(Point[] contour, Vector3 center3D)
    {
        OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contour);

        Texture2D cameraTex = ColorSourceManager.Instance.GetColorTexture();

        if (cameraTex == null)
        {
            Debug.LogWarning("Failed to read camera texture! No ColorSourceManager in Scene?");
            return null;
        }

        Mat colorMat = Texture2DToMat(cameraTex);

        Texture2D camSegment = GetColorSegmentFromDepthContour(
            _DepthManager.GetData(), boundingRect, _CoordinateMapper, colorMat,
            _DepthWidth, _DepthHeight, ColorSourceManager.Instance.GetColorTexture().width, ColorSourceManager.Instance.GetColorTexture().height
        );

        if (camSegment != null)
        {
            QRCodeVisualizerMesh.material.mainTexture = camSegment;

            // Instantiate object at calculated 3D center
            return GetPrefabFromQRCode(camSegment, center3D, Quaternion.identity, depthTextureVisualDestinationMesh.transform);
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        if (_SpawnedObjects == null || _SpawnedObjects.Count <= 0) return;

        Gizmos.color = Color.red;
        foreach (GameObject obj in _SpawnedObjects)
        {
            if (obj == null) continue;

            Vector3 worldPosition = obj.transform.position;
            Gizmos.DrawWireSphere(worldPosition, 1f);
        }

        if (trackedObjects == null) return;

        Gizmos.color = Color.green;
        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key == null) continue;
            if (kvp.Value == null) continue;

            Vector3 worldPosition = kvp.Key.transform.position;

            Gizmos.DrawWireSphere(worldPosition, maxAllowedMovementRadius);
        }
    }

    public static Mat Texture2DToMat(Texture2D texture)
    {
        if (texture == null || !texture.isReadable)
        {
            Debug.LogError("Texture is null or not readable.");
            return null;
        }

        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        Mat mat = new Mat(height, width, MatType.CV_8UC4); // RGBA

        unsafe
        {
            byte* dstPtr = (byte*)mat.DataPointer;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 color = pixels[i];
                dstPtr[i * 4 + 0] = color.b;
                dstPtr[i * 4 + 1] = color.g;
                dstPtr[i * 4 + 2] = color.r;
                dstPtr[i * 4 + 3] = color.a;
            }
        }

        return mat;
    }

    public Texture2D GetColorSegmentFromDepthContour(ushort[] depthData, OpenCvSharp.Rect depthRect, CoordinateMapper coordinateMapper, Mat colorMat, 
                                                     int depthWidth, int depthHeight, int colorWidth,int colorHeight)
    {
        if (depthData == null || depthData.Length != depthWidth * depthHeight)
            throw new ArgumentException("Invalid depth data dimensions.");

        var colorSpacePoints = new ColorSpacePoint[depthData.Length];
        coordinateMapper.MapDepthFrameToColorSpace(depthData, colorSpacePoints);

        var colorPointsList = new List<Point>();

        for (int y = depthRect.Top; y < depthRect.Bottom; y++)
        {
            for (int x = depthRect.Left; x < depthRect.Right; x++)
            {
                if (x < 0 || x >= depthWidth || y < 0 || y >= depthHeight)
                    continue;

                int index = y * depthWidth + x;
                var csp = colorSpacePoints[index];

                if (float.IsNaN(csp.X) || float.IsNaN(csp.Y))
                    continue;

                int colorX = (int)Math.Round(csp.X);
                int colorY = (int)Math.Round(csp.Y);

                if (colorX >= 0 && colorX < colorWidth && colorY >= 0 && colorY < colorHeight)
                    colorPointsList.Add(new Point(colorX, colorY));
            }
        }

        if (colorPointsList.Count == 0)
            return null;

        OpenCvSharp.Rect colorBoundingRect = Cv2.BoundingRect(colorPointsList);

        // Clamp to color image bounds
        colorBoundingRect = new OpenCvSharp.Rect(
            Math.Clamp(colorBoundingRect.X, 0, colorWidth - 1),
            Math.Clamp(colorBoundingRect.Y, 0, colorHeight - 1),
            Math.Min(colorBoundingRect.Width, colorWidth - colorBoundingRect.X),
            Math.Min(colorBoundingRect.Height, colorHeight - colorBoundingRect.Y)
        );

        // Crop the Mat
        Mat croppedMat = new Mat(colorMat, colorBoundingRect);

        croppedMat = Sharpen(croppedMat);

        // Convert to Texture2D
        return MatToTexture2D(croppedMat);
    }

    private Texture2D MatToTexture2D(Mat mat)
    {
        if (mat == null || mat.Empty())
            return null;

        Mat rgbaMat = new Mat();

        // Convert to RGBA for Unity if needed
        if (mat.Type() == MatType.CV_8UC3)
        {
            Cv2.CvtColor(mat, rgbaMat, ColorConversionCodes.BGR2RGBA);
        }
        else if (mat.Type() == MatType.CV_8UC4)
        {
            Cv2.CvtColor(mat, rgbaMat, ColorConversionCodes.BGRA2RGBA); // Optional, but safest
        }
        else
        {
            Debug.LogError("Unsupported Mat type: " + mat.Type());
            return null;
        }

        Texture2D texture = new Texture2D(rgbaMat.Width, rgbaMat.Height, TextureFormat.RGBA32, false);
        byte[] data = new byte[rgbaMat.Rows * rgbaMat.Cols * 4];
        System.Runtime.InteropServices.Marshal.Copy(rgbaMat.Data, data, 0, data.Length);
        texture.LoadRawTextureData(data);
        texture.Apply();

        return texture;
    }

    private void InstantiateAndDestroyObjectsWithMesh()
    {
        ClearSpawnedObjects();

        HierarchyIndex[] hierarchy;
        Point[][] contours;
        Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        objectBounds.Clear();

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (minBlobSize <= area && area <= maxBlobSize)
            {
                // Convert 2D contour positions to Vector2 for simplification
                List<Vector2> contour2D = new List<Vector2>();

                for (int i = 0; i < contour.Length; i++)
                {
                    Vector3 pos = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
                    contour2D.Add(new Vector2(pos.x, pos.z));
                }

                Vector2[] simplified = SimplifyPolygon(contour2D.ToArray(), simplificationTolerance);

                if (simplified.Length < 3)
                    return;

                // Convert 2D contour positions to world positions
                worldPoints3D = new Vector3[simplified.Length];

                for (int i = 0; i < simplified.Length; i++)
                {
                    //worldPoints3D[i] = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
                    worldPoints3D[i].x = simplified[i].x;
                    worldPoints3D[i].z = simplified[i].y;
                    worldPoints3D[i].y = depthTextureVisualDestinationMesh.transform.position.y;
                }

                /*
                // Debug: Visualize contour in 3D
                for (int i = 0; i < worldPoints3D.Length; i++)
                {
                    Vector3 startPoint = worldPoints3D[i];
                    Vector3 endPoint = worldPoints3D[(i + 1) % worldPoints3D.Length];
                    Debug.DrawLine(startPoint, endPoint, Color.green);
                }
                */

                // Compute 3D bounds from worldPoints3D---
                Vector3 minBounds = worldPoints3D[0];
                Vector3 maxBounds = worldPoints3D[0];

                foreach (var point in worldPoints3D)
                {
                    minBounds = Vector3.Min(minBounds, point);
                    maxBounds = Vector3.Max(maxBounds, point);
                }

                Vector3 size3D = maxBounds - minBounds;
                Vector3 center3D = minBounds + size3D / 2f;
                objectBounds.Add(new Rect(minBounds.x, minBounds.z, size3D.x, size3D.z)); // Assuming x-z plane is top-down
                                                                                          //----------

                OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contour);

                Texture2D cameraTex = ColorSourceManager.Instance.GetColorTexture();

                if (cameraTex == null)
                {
                    Debug.LogWarning("Failed to read camera texture! No ColorSourceManager in Scene?");
                    return;
                }

                Mat colorMat = Texture2DToMat(cameraTex);

                Texture2D camSegment = GetColorSegmentFromDepthContour(
                    _DepthManager.GetData(), boundingRect, _CoordinateMapper, colorMat,
                    _DepthWidth, _DepthHeight, ColorSourceManager.Instance.GetColorTexture().width, ColorSourceManager.Instance.GetColorTexture().height
                );

                if (camSegment != null)
                {
                    QRCodeVisualizerMesh.material.mainTexture = camSegment;

                    // Instantiate object at calculated 3D center
                    GameObject obj = InstantiateFromQRCode(camSegment, center3D, Quaternion.identity, depthTextureVisualDestinationMesh.transform);

                    if (obj == null)
                        return;

                    GameObject childPlaneObj = obj.transform.GetChild(0).gameObject;

                    _SpawnedObjects.Add(obj);

                    Vector3 localCenter = depthTextureVisualDestinationMesh.transform.InverseTransformPoint(center3D);

                    if (visualizeWithLines)
                        VisualizeWorldPoints3D(obj, worldPoints3D);

                    Mesh planeMesh = TriangulateFull(worldPoints3D, localCenter, collisionWallHeight);
                    Mesh wallMesh = ExtrudePolygon(worldPoints3D, localCenter, collisionWallHeight);

                    if (wallMesh != null)
                    {
                        if (!childPlaneObj.TryGetComponent<MeshFilter>(out var meshFilter))
                            meshFilter = childPlaneObj.AddComponent<MeshFilter>();
                        meshFilter.mesh = planeMesh;

                        if (!childPlaneObj.TryGetComponent<MeshCollider>(out var childMeshCollider))
                            childMeshCollider = obj.AddComponent<MeshCollider>();
                        childMeshCollider.sharedMesh = planeMesh;

                        //------------

                        if (!obj.TryGetComponent<MeshRenderer>(out var meshRenderer))
                            meshRenderer = obj.AddComponent<MeshRenderer>();
                        meshRenderer.material = TopDownObjectPrefab3D.GetComponent<MeshRenderer>().sharedMaterial;

                        // Optional: Enable mesh collider
                        if (!obj.TryGetComponent<MeshCollider>(out var meshCollider))
                            meshCollider = obj.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = wallMesh;
                    }
                    else
                    {
                        Debug.LogWarning("Failed to create mesh for object with contour.");
                    }
                }
            }
        }
    }

    private GameObject InstantiateFromQRCode(Texture2D colorTex, Vector3 center3D, Quaternion identity, Transform transform)
    {
        GameObject prefabRef = null;

        string objectString = ReadQRCodeFromImage(colorTex, colorTex.width, colorTex.height);

        if (!TopDownObjectsPrefabs3D.dict.ContainsKey(objectString) || TopDownObjectsPrefabs3D.dict[objectString] == null)
        {
            Debug.LogWarning("No Key (" + objectString + ") from Dictionary found or empty GameObject reference!");
            return null;
        }

        prefabRef = TopDownObjectsPrefabs3D.dict[objectString];

        return Instantiate(
            prefabRef,
            center3D,
            Quaternion.identity,
            depthTextureVisualDestinationMesh.transform
        );
    }

    private GameObject GetPrefabFromQRCode(Texture2D colorTex, Vector3 center3D, Quaternion identity, Transform transform)
    {
        GameObject prefabRef = null;

        string objectString = ReadQRCodeFromImage(colorTex, colorTex.width, colorTex.height);

        if (!TopDownObjectsPrefabs3D.dict.ContainsKey(objectString) || TopDownObjectsPrefabs3D.dict[objectString] == null)
        {
            Debug.LogWarning("No Key (" + objectString + ") from Dictionary found or empty GameObject reference!");
            return null;
        }

        prefabRef = TopDownObjectsPrefabs3D.dict[objectString];

        return prefabRef;
    }

    private string ReadQRCodeFromImage(Texture2D tex, int width, int height)
    {
        // create a barcode reader instance
        var reader = new BarcodeReader
        {
            AutoRotate = true
        };

        try
        {
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(tex.GetPixels32(), width, height);

            // do something with the result
            if (result != null)
            {
                Console.WriteLine(result.BarcodeFormat.ToString());
                Console.WriteLine(result.Text);
                return result.Text;
            }
            else
            {
                Console.WriteLine("No barcode found");
                return "";
            }
        } 
        catch (Exception e)
        {
            return "";
        }
    }

    public Mesh TriangulateFull(Vector3[] points, Vector3 center, float height)
    {
        if (points.Length < 3) return null;

        // Project to 2D (XZ or XY depending on plane)
        Vector3[] points3D = new Vector3[points.Length];
        Vector2[] points2D = new Vector2[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 correct3DPoint = depthTextureVisualDestinationMesh.transform.InverseTransformPoint(worldPoints3D[i]);

            Vector3 local = correct3DPoint - center;

            points2D[i] = new Vector2(local.x, local.z);
            points3D[i] = local;
        }

        // Triangulate
        Triangulator tr = new Triangulator(points2D);
        int[] indices = tr.Triangulate();

        // Build mesh
        Mesh mesh = new Mesh();
        mesh.vertices = points3D;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh ExtrudePolygon(Vector3[] points, Vector3 center, float height)
    {
        int n = points.Length;
        float halfHeight = height / 2f;

        // Shift points relative to center
        Vector3[] top = new Vector3[n];
        Vector3[] bottom = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 correct3DPoint = depthTextureVisualDestinationMesh.transform.InverseTransformPoint(points[i]);
            Vector3 local = correct3DPoint - center;
            bottom[i] = local - Vector3.up * halfHeight;
            top[i] = local + Vector3.up * halfHeight;
        }

        // Prepare vertices (only top + bottom for walls)
        Vector3[] vertices = new Vector3[n * 2]; // bottom + top
        for (int i = 0; i < n; i++)
        {
            vertices[i] = bottom[i];
            vertices[i + n] = top[i];
        }

        List<int> triangles = new List<int>();

        // Loop through vertices and create side walls
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n; // Wrap around to form a loop

            int b0 = i;       // Bottom
            int b1 = next;    // Next bottom
            int t0 = i + n;   // Top
            int t1 = next + n; // Next top

            // Side wall (two triangles connecting bottom to top)
            // Flip the triangle winding order to flip the normals
            triangles.Add(b0); // Bottom
            triangles.Add(t1); // Next top
            triangles.Add(t0); // Top

            triangles.Add(b0); // Bottom
            triangles.Add(b1); // Next bottom
            triangles.Add(t1); // Next top
        }

        // Create mesh and set vertices and triangles
        Mesh mesh = new Mesh();
        mesh.name = "ExtrudedWalls";
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void VisualizeWorldPoints3D(GameObject obj, Vector3[] worldPoints3D)
    {
        // Create a GameObject to hold the LineRenderer
        if (!obj.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
        {
            lineRenderer = obj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Create a new array that includes the first point at the end
        Vector3[] closedLoopPoints = new Vector3[worldPoints3D.Length + 1];
        worldPoints3D.CopyTo(closedLoopPoints, 0);
        closedLoopPoints[closedLoopPoints.Length - 1] = worldPoints3D[0]; // close the loop

        // Set LineRenderer properties
        lineRenderer.positionCount = closedLoopPoints.Length;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Set the positions for the LineRenderer
        lineRenderer.SetPositions(closedLoopPoints);

        //lineRenderer.startColor = lineColor;
        //lineRenderer.endColor = lineColor;
    }

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

            List<Vector2> result = new List<Vector2>();

            int firstIndex = 0;
            int lastIndex = points.Length - 1;
            List<int> pointIndexesToKeep = new List<int> { firstIndex, lastIndex };

            while (lastIndex >= firstIndex && points[firstIndex] == points[lastIndex]) 
                lastIndex--; // Avoid duplicates

            Reduce(points, firstIndex, lastIndex, epsilon, pointIndexesToKeep);
            pointIndexesToKeep.Sort();

            
            foreach (int index in pointIndexesToKeep)
            {
                result.Add(points[index]);
            }
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

    Vector3 DepthToViewport(Vector2 depthPos)
    {
        float x = (depthPos.x / _DepthWidth) * 2 - 1;
        float y = (depthPos.y / _DepthHeight) * 2 - 1;

        x = -x;
        y = -y;

        return _MainCamera.ViewportToWorldPoint(new Vector3((x + 1) / 2, (y + 1) / 2, 0));
    }

    private void ClearSpawnedObjects()
    {
        foreach (var item in trackedObjects) Destroy(item.Key);
        foreach (GameObject obj in _SpawnedObjects) Destroy(obj);

        trackedObjects.Clear();
        objectBounds.Clear();
        _SpawnedObjects.Clear();
    }

    private void OnApplicationQuit()
    {
        if (_Sensor != null && _Sensor.IsOpen) _Sensor.Close();
        if (_BinaryImage != null) _BinaryImage.Dispose();
        if (_Labels != null) _Labels.Dispose();
        if (_Stats != null) _Stats.Dispose();
        if (_Centroids != null) _Centroids.Dispose();
    }
}