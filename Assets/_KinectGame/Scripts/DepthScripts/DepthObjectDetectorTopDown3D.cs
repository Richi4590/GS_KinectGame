using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;
using Windows.Kinect;
using Rect = UnityEngine.Rect;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Rendering;
using AYellowpaper.SerializedCollections;
using System;
using OpenCvSharp.Util;
using System.Runtime.InteropServices;
using System.IO;

public class DepthObjectDetectorTopDown3D : MonoBehaviour
{
    [Header("Object References")]
    public static DepthObjectDetectorTopDown3D Instance;
    public ContourApproximationModes ContourAproximationMode;
    public bool DEBUG_DEPTH_TEX = false;
    public bool DEBUG_CREATE_VISUAL_TEX = false;
    public string DEBUG_BINARY_IMAGE_PATH = "BinaryTest";
    public Camera _Camera;
    public GameObject DepthSourceManager;
    public SerializedDictionaryShapesEnumToGameObject TopDownObjectsPrefabs3DDict;
    public GameObject DebugObjectCenterPrefab;
    public MeshRenderer depthTextureVisualDestinationMesh;
    public Transform depthChildrenRootObject;
    public GameObject QRCodeVisualizer;
    public ObjectCreationMode currentMode = ObjectCreationMode.InstantiateObjectAndDestroyPerFrame;

    [Header("Depth Settings")]
    [Min(0)] public int MinDepth = 500;
    [Min(0)] public int MaxDepth = 1000;
    [Min(0)] public int minBlobSize = 10;
    [Min(0)] public int maxBlobSize = 600;
    [Min(0)] public int dilationIterations = 2;
    [Min(0)] public int erosionIterations = 2;
    [Min(1)] public int morhologicalSpecRemoval = 1;
    public float epsilonShapeRecognition = 0.005f;
    [Range(0, 100)] public int borderThickness = 0;
    [Min(0)] public float viewPortXOffset = 0.0f;
    [Min(0)] public float simplificationTolerance = 0.02f;
    [Min(0)] public float gaussianBlurAmount = 0;
    //[Min(0)] public int fixedSquareImageSizeArucoCode = 50;

    public LayerMask layersToIgnoreMeshUpdates;  

    [Header("Object Settings")]
    public bool flipSpriteX = false;
    public bool flipSpriteY = false;

    public bool flipDepthMapX = false;
    public bool flipDepthMapY = false;

    public float maxAllowedMovementRadius = 1f;
    public float collisionWallHeight = 5f;
    public float objectFeatureRadiusMultiplier = 1.0f;
    public bool visualizeWithLines = true;
    public float lineWidth = 0.1f;

    [Header("Shape Detection Settings")]
    [Header("Basic Shape Thresholds")]
    [Min(0)] public double circularityThreshold = 0.75;
    [Min(0)] public double solidityMinForBasicShapes = 0.9;

    [Header("Star Shape Detection")]
    [Min(0)] public double solidityStarThreshold = 0.85;

    [Header("Rectangle & Square Detection")]
    [Min(0)] public double squareAspectMin = 0.85;
    [Min(0)] public double squareAspectMax = 1.15;
    [Min(0)] public double extentRectMin = 0.65;
    [Min(0)] public double extentSquareMin = 0.75;

    [Header("Triangle Detection")]
    [Min(0)] public double extentTriangleMin = 0.5;
    [Min(0)] public double extentTriangleMax = 0.75;

    [Header("Hu Moment Settings")]
    [Min(0)] public bool useHuMoments = false;
    [Min(0)] public double huCircleThreshold = 0.3; // Optional: lower = more circular
    [Min(0)] public double huStarThreshold = 0.25;  // Optional: higher = more "star"-like

    [Header("Contour Area Filtering")]
    [Min(0)] public double minContourArea = 100;    // Ignore tiny noisy shapes

    public int framesToWait = 10;
    public bool SaveBinaryImage = false;

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

    private Camera _OrthographicCamera;
    private Mat _BinaryImage;
    private Mat _Labels;
    private Mat _Stats;
    private Mat _Centroids;
    private Vector3[] worldPoints3D;
    private Mat depthMat;
    double scaleX;
    double scaleY;
    private bool DEBUG_FloodFill_Mask = false;

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
        _OrthographicCamera = _Camera;

        if (TopDownObjectsPrefabs3DDict == null)
            TopDownObjectsPrefabs3DDict = GetComponent<SerializedDictionaryShapesEnumToGameObject>();

        // Allocate OpenCV Mats only once
        _BinaryImage = new Mat(_DepthHeight, _DepthWidth, MatType.CV_8UC1);
        _Labels = new Mat();
        _Stats = new Mat();
        _Centroids = new Mat();
        objectBounds = new List<Rect>();
        depthMat = new Mat(_DepthHeight, _DepthWidth, MatType.CV_16UC1);
        previousMode = currentMode;
        StartCoroutine(ExecuteEveryNFrames());
        
    }

    void Update()
    {

    }

    // Coroutine that will execute every N frames
    IEnumerator ExecuteEveryNFrames()
    {
        while (!_Sensor.IsOpen)
            yield return null;

        while (true)
        {
            // Wait for N frames
            yield return new WaitForSeconds(1f / ((int)(1.0f / Time.smoothDeltaTime)) * framesToWait);

            if (!DEBUG_DEPTH_TEX)
            {
                ProcessDepthData(_DepthManager.GetData());
            }
            else
            {
                _BinaryMaskTexture = Resources.Load<Texture2D>(DEBUG_BINARY_IMAGE_PATH);
                _BinaryMaskTexture = MakeReadable(_BinaryMaskTexture);

                int width = _BinaryMaskTexture.width;
                int height = _BinaryMaskTexture.height;

                Color[] pixelsInitial = _BinaryMaskTexture.GetPixels();

                if (flipDepthMapX)
                {
                    // Flip horizontally
                    for (int y = 0; y < height; y++)
                    {
                        int rowStart = y * width;
                        int rowEnd = rowStart + width - 1;
                        for (int x = 0; x < width / 2; x++)
                        {
                            int left = rowStart + x;
                            int right = rowEnd - x;

                            (pixelsInitial[left], pixelsInitial[right]) = (pixelsInitial[right], pixelsInitial[left]);
                        }
                    }
                }

                if (flipDepthMapY)
                {
                    // Flip vertically
                    for (int y = 0; y < height / 2; y++)
                    {
                        int topRow = y * width;
                        int bottomRow = (height - 1 - y) * width;

                        for (int x = 0; x < width; x++)
                        {
                            int topIndex = topRow + x;
                            int bottomIndex = bottomRow + x;

                            (pixelsInitial[topIndex], pixelsInitial[bottomIndex]) = (pixelsInitial[bottomIndex], pixelsInitial[topIndex]);
                        }
                    }
                }

                _BinaryMaskTexture.SetPixels(pixelsInitial);
                _BinaryMaskTexture.Apply(false); // 'false' skips mipmap updates for speed

                //---

                Color32[] pixels = _BinaryMaskTexture.GetPixels32();

                if (_BinaryMaskTexture != null & (_BinaryMaskTexture.width > 0 & _BinaryMaskTexture.height > 0))
                {
                    byte[] grayscalePixels = new byte[pixels.Length];

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        // Convert to grayscale by red channel or average RGB
                        byte gray = (byte)(0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b);

                        // Optional: binarize (threshold) to black/white
                        grayscalePixels[i] = (gray > 127) ? (byte)255 : (byte)0;
                    }

                    // Copy to Mat
                    _BinaryImage.SetArray(0, 0, grayscalePixels);

                    if (gaussianBlurAmount > 0)
                    {
                        int intVal = (int)Math.Ceiling(gaussianBlurAmount);
                        int oddBlurAmount = (intVal % 2 == 0) ? intVal + 1 : intVal;

                        Cv2.GaussianBlur(_BinaryImage, _BinaryImage, new Size(oddBlurAmount, oddBlurAmount), 1); // Optional
                    }
                    depthTextureVisualDestinationMesh.material.mainTexture = _BinaryMaskTexture;
                }

            }
            
            if (SaveBinaryImage)
            {
                SaveBinaryImage = false;
                SaveToResources(_BinaryImage, "binary_mask_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".png");
            }
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

    Texture2D MakeReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width, source.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
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
            if (area >= minBlobSize && area <= maxBlobSize)
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
        GCHandle handle = GCHandle.Alloc(depthData, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            depthMat.SetArray(0, 0, depthData); // Option 1 (slightly higher-level)
                                                // OR:
                                                // Marshal.Copy(depthData, 0, depthMat.Data, depthData.Length); // Option 2 (low-level fallback)
        }
        finally
        {
            handle.Free();
        }


        if (flipDepthMapX)
            Cv2.Flip(depthMat, depthMat, FlipMode.X);

        if (flipDepthMapY)
            Cv2.Flip(depthMat, depthMat, FlipMode.Y);

        Cv2.InRange(depthMat, MinDepth, MaxDepth, _BinaryImage);

        // Remove edge-connected components
        RemoveEdgeArtifacts(ref _BinaryImage);

        if (gaussianBlurAmount > 0)
            Cv2.GaussianBlur(_BinaryImage, _BinaryImage, new Size(gaussianBlurAmount, gaussianBlurAmount), 1); // Optional

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

        if (borderThickness >= 1)
        {
            // Top border
            modifiedMask[new OpenCvSharp.Rect(0, 0, modifiedMask.Cols, borderThickness)]
                .SetTo(new Scalar(colorValue));

            // Bottom border
            modifiedMask[new OpenCvSharp.Rect(0, modifiedMask.Rows - borderThickness, modifiedMask.Cols, borderThickness)]
                .SetTo(new Scalar(colorValue));

            // Left border
            modifiedMask[new OpenCvSharp.Rect(0, 0, borderThickness, modifiedMask.Rows)]
                .SetTo(new Scalar(colorValue));

            // Right border
            modifiedMask[new OpenCvSharp.Rect(modifiedMask.Cols - borderThickness, 0, borderThickness, modifiedMask.Rows)]
                .SetTo(new Scalar(colorValue));

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

        // Optional smoothing to help with rough masks
        //Cv2.GaussianBlur(modifiedMask, modifiedMask, new Size(5, 5), 0);

        // Remove small specks
        //Cv2.MorphologyEx(modifiedMask, modifiedMask, MorphTypes.Open, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(morhologicalSpecRemoval, morhologicalSpecRemoval)));

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
            if (area <= minBlobSize || area >= maxBlobSize)
                continue;

            // Convert to simplified 2D world contour
            List<OpenCvSharp.Point> contour2D = new List<OpenCvSharp.Point>();

            for (int i = 0; i < contour.Length; i++)
            {
                Vector3 pos = DepthToViewportMapped(new Vector2(contour[i].X, contour[i].Y));
                contour2D.Add(new OpenCvSharp.Point(pos.x, pos.z));
            }

            Point[] simplified = Cv2.ApproxPolyDP(contour2D, simplificationTolerance, true);
            //Vector2[] simplified = SimplifyPolygon(contour2D.ToArray(), simplificationTolerance);

            if (simplified.Length < 3)
                return;

            // Create 3D positions on XZ plane
            worldPoints3D = new Vector3[simplified.Length];

            for (int i = 0; i < simplified.Length; i++)
            {
                worldPoints3D[i] = new Vector3(
                    simplified[i].X,
                    depthChildrenRootObject.transform.position.y,
                    simplified[i].Y
                );

                //worldPoints3D[i] = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
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
                //GameObject newlyScannedPrefab = TopDownObjectsPrefabs3DDict.dict[0];//GetGameObjectPrefabFromCameraContour(contour, center3D);
                GameObject newlyScannedPrefab = GetGameObjectPrefabFromCameraContour(contour, center3D);
                
                closestObject.TryGetComponent<CustomTag>(out CustomTag t1);
                newlyScannedPrefab.TryGetComponent<CustomTag>(out CustomTag t2);

                if ((t1 != null && t2 != null) && (t1.tag != t2.tag)) //replace old object with the actual correct one
                {
                    trackedObjects.Remove(closestObject);
                    _SpawnedObjects.Remove(closestObject);
                    Destroy(closestObject);

                    closestObject = Instantiate(
                       newlyScannedPrefab,
                       center3D,
                       Quaternion.identity,
                       depthChildrenRootObject
                    );

                    closestObject.transform.localScale = depthTextureVisualDestinationMesh.transform.localScale;
                }

                // Reuse object
                obj = closestObject;
                obj.transform.position = center3D;
                trackedObjects[obj] = newCenter2D;

                float longestObjectSide = Mathf.Max(size3D.x, size3D.y);
                longestObjectSide = Mathf.Max(longestObjectSide, size3D.z) * 0.5f * objectFeatureRadiusMultiplier;
                SerializedDictionaryShapesEnumToGameObject.SetObjectPrefabParameters(closestObject, longestObjectSide);

                if (!_SpawnedObjects.Contains(obj))
                    _SpawnedObjects.Add(obj);

                updatedObjects.Add(obj);
                VisualizeWorldPoints3D(obj, worldPoints3D);
            }
            else
            {
                obj = InstantiateGameObjectFromCameraContour(contour, center3D);

                float longestObjectSide = Mathf.Max(size3D.x, size3D.y);
                longestObjectSide = Mathf.Max(longestObjectSide, size3D.z) * 0.5f * objectFeatureRadiusMultiplier;
                SerializedDictionaryShapesEnumToGameObject.SetObjectPrefabParameters(obj, longestObjectSide);

                if (obj != null) 
                {
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
                        meshRenderer.material = obj.GetComponent<MeshRenderer>().sharedMaterial;

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

    private GameObject InstantiateGameObjectFromCameraContour(Point[] contour, Vector3 center3D)
    {
        /*
        Texture2D cameraTex = ColorSourceManager.Instance.GetColorTexture();

        if (cameraTex == null)
        {
            Debug.LogWarning("Failed to read camera texture! No ColorSourceManager in Scene?");
            return null;
        }
        OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contour);


        Rect colorRect;

        colorRect = MapDepthRectToColorSpace(boundingRect,
                                          _CoordinateMapper,
                                          _DepthManager.GetData(), _DepthWidth);

        colorRect = MapCenteredDepthRectToColorSpace(boundingRect, _CoordinateMapper,
            _DepthManager.GetData(), cameraTex.width, cameraTex.height, _DepthWidth, _DepthHeight, fixedSquareImageSizeArucoCode);

        if (float.IsInfinity(colorRect.x) || float.IsInfinity(colorRect.y)
        || float.IsInfinity(colorRect.width) || float.IsInfinity(colorRect.height))
            return null;


        Texture2D camSegment = GetColorSegmentGPU(cameraTex, colorRect);

        if (camSegment != null)
        {
            if (QRCodeVisualizer.TryGetComponent<Image>(out Image img))
                img.material.SetTexture("_MainTex", (Texture)camSegment);
        }


        // Instantiate object at calculated 3D center
        GameObject obj = InstantiateFromArucoCode(camSegment, center3D, Quaternion.identity, depthTextureVisualDestinationMesh.transform);
        */

        string shape = ClassifyShape(
                                    contour,
                                    circularityThreshold,
                                    solidityStarThreshold,
                                    extentTriangleMin,
                                    extentTriangleMax,
                                    squareAspectMin,
                                    squareAspectMax,
                                    extentRectMin,
                                    extentSquareMin,
                                    solidityMinForBasicShapes
                                );
        bool wasParsable = Enum.TryParse(shape, out ShapesEnum recognizedShape);
        
        GameObject obj = null;

        if (wasParsable)
        {
            //Debug.Log($"Detected shape: {recognizedShape.ToString()}, " + (int)recognizedShape);

            if (!TopDownObjectsPrefabs3DDict.dict.ContainsKey(recognizedShape))
            {
                obj = Instantiate(
                        TopDownObjectsPrefabs3DDict.dict[ShapesEnum.Default],
                        center3D,
                        Quaternion.identity,
                        depthChildrenRootObject
                        );
            }
            else
            {
                obj = Instantiate(
                    TopDownObjectsPrefabs3DDict.dict[recognizedShape],
                    center3D,
                    Quaternion.identity,
                    depthChildrenRootObject
                    );
            }
        }
        else
        {
            //Debug.Log($"Could not parse enum! Is there an enum entry missing? ({shape})");

            obj = Instantiate(
                TopDownObjectsPrefabs3DDict.dict[ShapesEnum.Default],
                center3D,
                Quaternion.identity,
                depthChildrenRootObject
                );

            //Debug.Log(null);
        }

        obj.transform.localScale = depthTextureVisualDestinationMesh.transform.localScale;

        return obj;
    }

    private GameObject GetGameObjectPrefabFromCameraContour(Point[] contour, Vector3 center3D)
    {
        /*
        Texture2D cameraTex = ColorSourceManager.Instance.GetColorTexture();

        if (cameraTex == null)
        {
            Debug.LogWarning("Failed to read camera texture! No ColorSourceManager in Scene?");
            return null;
        }

        OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contour);

        Rect colorRect;
        */

        //
        //colorRect = MapDepthRectToColorSpace(boundingRect,
        //                                  _CoordinateMapper,
        //                                  _DepthManager.GetData(), _DepthWidth);

        /*
        colorRect = MapCenteredDepthRectToColorSpace(boundingRect, _CoordinateMapper,
            _DepthManager.GetData(), cameraTex.width, cameraTex.height, _DepthWidth, _DepthHeight, fixedSquareImageSizeArucoCode);


        if (float.IsInfinity(colorRect.x) || float.IsInfinity(colorRect.y)
        || float.IsInfinity(colorRect.width) || float.IsInfinity(colorRect.height))
            return null;
         
        Texture2D camSegment = GetColorSegmentGPU(cameraTex, colorRect);
         
        if (camSegment != null)
        {
            if (QRCodeVisualizer.TryGetComponent<Image>(out Image img))
                img.material.SetTexture("_MainTex", (Texture)camSegment);

            // Instantiate object at calculated 3D center
            return GetPrefabFromArucoCode(camSegment, center3D, Quaternion.identity, depthTextureVisualDestinationMesh.transform);
        }
        */

        string shape = ClassifyShape(
                                    contour,
                                    circularityThreshold,
                                    solidityStarThreshold,
                                    extentTriangleMin,
                                    extentTriangleMax,
                                    squareAspectMin,
                                    squareAspectMax,
                                    extentRectMin,
                                    extentSquareMin,
                                    solidityMinForBasicShapes
                                );
        bool wasParsable = Enum.TryParse(shape, out ShapesEnum recognizedShape);

        if (wasParsable)
        {
            //Debug.Log($"Detected shape: {recognizedShape.ToString()}, " + (int)recognizedShape);

            if (!TopDownObjectsPrefabs3DDict.dict.ContainsKey(recognizedShape))
                return TopDownObjectsPrefabs3DDict.dict[ShapesEnum.Default];
            else
                return TopDownObjectsPrefabs3DDict.dict[recognizedShape];
        }
        else
        {
            //Debug.Log($"Could not parse enum! Is there an enum entry missing? ({shape})");
            return TopDownObjectsPrefabs3DDict.dict[ShapesEnum.Default];
        }
    }

    public Rect MapCenteredDepthRectToColorSpace(OpenCvSharp.Rect boundingRect,
                                                        CoordinateMapper coordMapper,
                                                        ushort[] depthData,
                                                        int colorWidth,
                                                        int colorHeight,
                                                        int depthWidth,
                                                        int depthHeight,
                                                        int fixedSize = 50)
    {
        // Get center of the bounding rect
        int centerX = boundingRect.X + boundingRect.Width / 2;
        int centerY = boundingRect.Y + boundingRect.Height / 2;

        // Clamp to depth image bounds
        centerX = Mathf.Clamp(centerX, 0, depthWidth - 1);
        centerY = Mathf.Clamp(centerY, 0, depthHeight - 1);

        int depthIndex = centerY * depthWidth + centerX;
        ushort depthVal = depthData[depthIndex];

        // Map center depth point to color space
        DepthSpacePoint depthCenter = new DepthSpacePoint { X = centerX, Y = centerY };
        ColorSpacePoint colorCenter = coordMapper.MapDepthPointToColorSpace(depthCenter, depthVal);

        // Build fixed-size rect around the color-mapped center
        float halfSize = fixedSize / 2f;
        float colorX = colorCenter.X - halfSize;
        float colorY = colorCenter.Y - halfSize;

        // Clamp position so that the rect fits entirely within the image
        colorX = Mathf.Clamp(colorX, 0, colorWidth - fixedSize);
        colorY = Mathf.Clamp(colorY, 0, colorHeight - fixedSize);

        return new Rect(colorX, colorY, fixedSize, fixedSize);
    }

    Point[] UpsampleContour(Point[] contour, int targetPoints = 64)
    {
        if (contour.Length < 2)
            return contour;

        // Step 1: Calculate segment lengths and total length
        List<double> segmentLengths = new List<double>();
        double totalLength = 0;

        for (int i = 0; i < contour.Length; i++)
        {
            Point p1 = contour[i];
            Point p2 = contour[(i + 1) % contour.Length]; // Loop back to start
            double segLen = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            segmentLengths.Add(segLen);
            totalLength += segLen;
        }

        // Step 2: Interpolate targetPoints evenly along the full length
        List<Point> upsampled = new List<Point>();
        double step = totalLength / targetPoints;
        double distAlong = 0;
        int segIndex = 0;
        double segOffset = 0;

        for (int i = 0; i < targetPoints; i++)
        {
            while (segIndex < segmentLengths.Count && segOffset + segmentLengths[segIndex] < distAlong)
            {
                segOffset += segmentLengths[segIndex];
                segIndex++;
            }

            if (segIndex >= contour.Length)
                break;

            Point p1 = contour[segIndex];
            Point p2 = contour[(segIndex + 1) % contour.Length];
            double segLen = segmentLengths[segIndex];
            double t = (segLen == 0) ? 0 : (distAlong - segOffset) / segLen;

            int x = (int)Math.Round(p1.X + t * (p2.X - p1.X));
            int y = (int)Math.Round(p1.Y + t * (p2.Y - p1.Y));
            upsampled.Add(new Point(x, y));

            distAlong += step;
        }

        return upsampled.ToArray();
    }

    public Rect MapDepthRectToColorSpace(OpenCvSharp.Rect depthRect, CoordinateMapper coordMapper, ushort[] depthData, int depthWidth)
    {
        Vector2 topLeftColor = MapDepthPointToColor(depthRect.X, depthRect.Y);
        Vector2 bottomRightColor = MapDepthPointToColor(depthRect.X + depthRect.Width, depthRect.Y + depthRect.Height);

        return Rect.MinMaxRect(topLeftColor.x, topLeftColor.y, bottomRightColor.x, bottomRightColor.y);

        Vector2 MapDepthPointToColor(float x, float y)
        {
            int px = Mathf.Clamp(Mathf.RoundToInt(x), 0, depthWidth - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(y), 0, depthData.Length / depthWidth - 1);
            int depthIndex = py * depthWidth + px;

            ushort depthVal = depthData[depthIndex];

            DepthSpacePoint depthPoint = new DepthSpacePoint { X = px, Y = py };
            ColorSpacePoint colorPoint = coordMapper.MapDepthPointToColorSpace(depthPoint, depthVal);

            return new Vector2(colorPoint.X, colorPoint.Y);
        }
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

        // Allocate Mat with 4 channels (RGBA)
        Mat mat = new Mat(height, width, MatType.CV_8UC4);
        byte[] rawBytes = new byte[pixels.Length * 4];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            int offset = i * 4;
            rawBytes[offset + 0] = c.b; // OpenCV uses BGRA
            rawBytes[offset + 1] = c.g;
            rawBytes[offset + 2] = c.r;
            rawBytes[offset + 3] = c.a;
        }

        unsafe
        {
            Marshal.Copy(rawBytes, 0, mat.Data, rawBytes.Length);
        }

        return mat;
    }

    public Texture2D GetColorSegmentGPU(Texture2D source, Rect cropRect)
    {
        RenderTexture prev = RenderTexture.active;

        RenderTexture rt = new RenderTexture(source.width, source.height, 0);

        Graphics.Blit(source, rt); // draw source texture into RT

        RenderTexture.active = rt;

        Texture2D cropped = new Texture2D((int)cropRect.width, (int)cropRect.height, TextureFormat.RGB24, false);

        cropped.ReadPixels(cropRect, 0, 0);
        cropped.Apply();

        RenderTexture.active = prev;
        rt.Release();

        return cropped;
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

        Texture2D texture = new Texture2D(rgbaMat.Width, rgbaMat.Height, TextureFormat.RGB24, false);
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
        Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourAproximationMode);

        objectBounds.Clear();

        foreach (Point[] contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area >= minBlobSize && area <= maxBlobSize)
            {
                // Convert to simplified 2D world contour
                List<OpenCvSharp.Point> contour2D = new List<OpenCvSharp.Point>();

                for (int i = 0; i < contour.Length; i++)
                {
                    Vector3 pos = DepthToViewportMapped(new Vector2(contour[i].X, contour[i].Y));
                    contour2D.Add(new OpenCvSharp.Point(pos.x, pos.z));
                }

                Point[] simplified = Cv2.ApproxPolyDP(contour2D, simplificationTolerance, true);
                //Vector2[] simplified = SimplifyPolygon(contour2D.ToArray(), simplificationTolerance);
                
                if (simplified.Length < 3)
                    return;

                // Convert 2D contour positions to world positions
                worldPoints3D = new Vector3[simplified.Length];

                for (int i = 0; i < simplified.Length; i++)
                {
                    worldPoints3D[i] = new Vector3(
                        simplified[i].X,
                        depthChildrenRootObject.transform.position.y,
                        simplified[i].Y
                    );

                    //worldPoints3D[i] = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
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
                float longestObjectSide = Mathf.Max(size3D.x, size3D.y);
                longestObjectSide = Mathf.Max(longestObjectSide, size3D.z) * 0.5f * objectFeatureRadiusMultiplier;

                // Instantiate object at calculated 3D center
                GameObject obj = InstantiateGameObjectFromCameraContour(contour, center3D);

                SerializedDictionaryShapesEnumToGameObject.SetObjectPrefabParameters(obj, longestObjectSide);

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
                    meshRenderer.material = obj.GetComponent<MeshRenderer>().sharedMaterial;

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

    /*
    private GameObject InstantiateFromArucoCode(Texture2D colorTex, Vector3 center3D, Quaternion identity, Transform transform)
    {
        GameObject prefabRef = GetPrefabFromArucoCode(colorTex, center3D, identity, transform);

        return Instantiate(
            prefabRef,
            center3D,
            Quaternion.identity,
            depthChildrenRootObject
        );
    }


    private GameObject GetPrefabFromArucoCode(Texture2D colorTex, Vector3 center3D, Quaternion identity, Transform transform)
    {
        GameObject prefabRef = null;

        int objectID = arucoIDRetriever.ReadIDFromImage(colorTex);
        //Debug.Log(objectID);

        if (!TopDownObjectsPrefabs3DDict.dict.ContainsKey(objectID) || TopDownObjectsPrefabs3DDict.dict[objectID] == null)
        {
            //Debug.LogWarning("No Key (" + objectID + ") from Dictionary found or empty GameObject reference!");
            prefabRef = TopDownObjectsPrefabs3DDict.dict[0];
        }
        else
        {
            prefabRef = TopDownObjectsPrefabs3DDict.dict[objectID];
        }

        return prefabRef;
    }
    */
    

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

    Vector3 DepthToViewport(Vector2 depthPos)
    {
        float x = (depthPos.x / _DepthWidth) * 2 - 1;
        float y = (depthPos.y / _DepthHeight) * 2 - 1;

        x = -x;
        y = -y;

        return _OrthographicCamera.ViewportToWorldPoint(new Vector3((x + 1) / 2, (y + 1) / 2, 0));
    }

    Vector3 DepthToViewportMapped(Vector2 depthPos)
    {
        depthPos.x = -depthPos.x;

        float depthWidth = _DepthWidth;   // e.g., 512
        float depthHeight = _DepthHeight; // e.g., 424

        float camAspect = _OrthographicCamera.aspect; // Unity camera (e.g. 16:9 = 1.777)
        float depthAspect = depthWidth / depthHeight; // Kinect (e.g. 512 / 424 ≈ 1.2)

        float xNorm = depthPos.x / depthWidth;
        float yNorm = depthPos.y / depthHeight;

        float viewportX, viewportY;

        if (camAspect > depthAspect)
        {
            // 16:9 camera is wider — add pillarbox
            float pillarbox = (1f - (depthAspect / camAspect)) ;
            float scaleX = depthAspect / camAspect;
            viewportX = pillarbox + xNorm * scaleX;
            viewportY = 1f - yNorm;
            viewportX = viewportX + viewPortXOffset; //- pillarbox to offset it back to center
        }
        else
        {
            // 4:3 camera is taller — add letterbox
            float letterbox = (1f - (camAspect / depthAspect)) / 2f;
            float scaleY = camAspect / depthAspect;
            viewportX = xNorm;
            viewportY = letterbox + (1f - yNorm) * scaleY;
        }

        return _OrthographicCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, 0));
    }

    double GetAngle(Point a, Point b, Point c)
    {
        // Vectors: ba and bc
        double abx = a.X - b.X;
        double aby = a.Y - b.Y;
        double cbx = c.X - b.X;
        double cby = c.Y - b.Y;

        double dot = abx * cbx + aby * cby;
        double cross = abx * cby - aby * cbx;

        double angle = Math.Atan2(Math.Abs(cross), dot); // Always positive angle
        return angle * (180.0 / Math.PI); // Convert to degrees
    }

    bool IsApproximately(double a, double b, double tolerance)
    {
        return Math.Abs(a - b) < tolerance;
    }

    List<double> GetAllInteriorAngles(Point[] contour)
    {
        List<double> angles = new();
        int n = contour.Length;
        for (int i = 0; i < n; i++)
        {
            Point a = contour[(i - 1 + n) % n];
            Point b = contour[i];
            Point c = contour[(i + 1) % n];
            angles.Add(GetAngle(a, b, c));
        }
        return angles;
    }

    string ClassifyShape_OLD(Point[] approx, Point[] fullContour, double angleToleranceDeviation = 10, double aspectTolerance = 0.2)
    {
        int vertices = approx.Length;
        bool isConvex = Cv2.IsContourConvex(approx);

        if (vertices == 3)
            return "Triangle";

        if (vertices == 4)
        {
            OpenCvSharp.Rect rect = Cv2.BoundingRect(approx);
            float aspectRatio = (float)rect.Width / rect.Height;

            double angle1 = GetAngle(approx[0], approx[1], approx[2]);
            double angle2 = GetAngle(approx[1], approx[2], approx[3]);
            double angle3 = GetAngle(approx[2], approx[3], approx[0]);
            double angle4 = GetAngle(approx[3], approx[0], approx[1]);

            bool allRight = IsApproximately(angle1, 90, angleToleranceDeviation) &&
                            IsApproximately(angle2, 90, angleToleranceDeviation) &&
                            IsApproximately(angle3, 90, angleToleranceDeviation) &&
                            IsApproximately(angle4, 90, angleToleranceDeviation);

            if (allRight)
            {
                return (aspectRatio > (1 - aspectTolerance) && aspectRatio < (1 + aspectTolerance))
                    ? "Square"
                    : "Rectangle";
            }

            bool oppAnglesEqual = IsApproximately(angle1, angle3, angleToleranceDeviation) &&
                                  IsApproximately(angle2, angle4, angleToleranceDeviation);

            if (oppAnglesEqual && !allRight)
                return "Parallelogram";

            return "Trapezoid";
        }

        if (!isConvex && vertices == 10)
            return "Star";

        if (vertices > 4)
        {
            double area = Cv2.ContourArea(approx);
            double peri = Cv2.ArcLength(approx, true);
            double circularity = 4 * Math.PI * area / (peri * peri);
            if (circularity > 0.8)
                return "Circle";
        }

        return "Default";
    }

    /*
    public string ClassifyShape_V2(Point[] contour, double angleDeviation = 15, double circularityThreshold = 0.75)
    {
        if (contour.Length < 5) return "Default";

        // 1. Circularity test
        double area = Cv2.ContourArea(contour);
        double perimeter = Cv2.ArcLength(contour, true);
        double circularity = 4 * Math.PI * area / (perimeter * perimeter);
        if (circularity > circularityThreshold)
            return "Circle";

        // 2. Bounding box for square/rectangle test
        var rect = Cv2.BoundingRect(contour);
        float aspectRatio = (float)rect.Width / rect.Height;

        // 3. Triangle/Trapezoid via angle distribution (fallback, rough)
        int triangleLike = CountTrueCornerAngles(contour, minAngleDistribution, maxAngleDistribution, sharpnessThresholdDeg);
        if (triangleLike >= 3)
            return "Triangle";

        //4. Rectangle/Square
        if (aspectRatio > 0.85f && aspectRatio < 1.15f)
            return "Square";
        if (aspectRatio > 1.15f || aspectRatio < 0.85f)
            return "Rectangle";

        // 3. Sharp angles for star-like shapes
        int sharpCorners = CountSharpCorners(contour, angleDeviation);
        if (sharpCorners >= 8 && sharpCorners <= 12)
            return "Star";

        return "Default";
    }
    */

    public string ClassifyShape(
        Point[] contour,
        double circularityThreshold = 0.75,
        double solidityStarThreshold = 0.85,
        double extentTriangleMin = 0.5,
        double extentTriangleMax = 0.75,
        double squareAspectMin = 0.85,
        double squareAspectMax = 1.15,
        double extentRectMin = 0.65,
        double extentSquareMin = 0.75,
        double solidityMinForBasicShapes = 0.9
    )
    {
        if (contour.Length < 5)
            return "Default";

        // === Region Properties ===
        double area = Cv2.ContourArea(contour);
        double perimeter = Cv2.ArcLength(contour, true);
        if (area <= 0 || perimeter <= 0)
            return "Default";

        double circularity = 4 * Math.PI * area / (perimeter * perimeter);

        var rect = Cv2.BoundingRect(contour);
        double aspectRatio = (double)rect.Width / rect.Height;
        double extent = area / (rect.Width * rect.Height);

        Point[] hull = Cv2.ConvexHull(contour);
        double hullArea = Cv2.ContourArea(hull);
        double solidity = area / hullArea;

        double[] huLog = null;
        if (useHuMoments)
        {
            var moments = Cv2.Moments(contour);
            var hu = ComputeHuMoments(moments);
            huLog = hu.Select(h => -1 * Math.Sign(h) * Math.Log10(Math.Abs(h) + 1e-30)).ToArray();
        }

        // === Classification Logic ===

        if (circularity > circularityThreshold && solidity > solidityMinForBasicShapes)
            return "Circle";

        if (extent > extentSquareMin && aspectRatio >= squareAspectMin && aspectRatio <= squareAspectMax && solidity > solidityMinForBasicShapes)
            return "Square";

        if (extent > extentRectMin && solidity > solidityMinForBasicShapes &&
            (aspectRatio < squareAspectMin || aspectRatio > squareAspectMax))
            return "Rectangle";

        //if (solidity < solidityStarThreshold && extent < extentTriangleMin)
            //return "Star";

        if (solidity > solidityMinForBasicShapes && extent >= extentTriangleMin && extent <= extentTriangleMax)
            return "Triangle";

        return "Default";
    }

    public static double[] ComputeHuMoments(Moments m)
    {
        double n20 = m.Nu20;
        double n02 = m.Nu02;
        double n11 = m.Nu11;
        double n30 = m.Nu30;
        double n12 = m.Nu12;
        double n21 = m.Nu21;
        double n03 = m.Nu03;

        double[] hu = new double[7];

        hu[0] = n20 + n02;
        hu[1] = Math.Pow(n20 - n02, 2) + 4 * Math.Pow(n11, 2);
        hu[2] = Math.Pow(n30 - 3 * n12, 2) + Math.Pow(3 * n21 - n03, 2);
        hu[3] = Math.Pow(n30 + n12, 2) + Math.Pow(n21 + n03, 2);
        hu[4] = (n30 - 3 * n12) * (n30 + n12) * (Math.Pow(n30 + n12, 2) - 3 * Math.Pow(n21 + n03, 2)) +
                (3 * n21 - n03) * (n21 + n03) * (3 * Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2));
        hu[5] = (n20 - n02) * (Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2)) +
                4 * n11 * (n30 + n12) * (n21 + n03);
        hu[6] = (3 * n21 - n03) * (n30 + n12) * (Math.Pow(n30 + n12, 2) - 3 * Math.Pow(n21 + n03, 2)) -
                (n30 - 3 * n12) * (n21 + n03) * (3 * Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2));

        return hu;
    }

    int CountSharpCorners(Point[] contour, double thresholdDeg)
    {
        int sharp = 0;
        for (int i = 0; i < contour.Length; i++)
        {
            Point a = contour[(i - 1 + contour.Length) % contour.Length];
            Point b = contour[i];
            Point c = contour[(i + 1) % contour.Length];

            double angle = GetAngleDeg(a, b, c);
            if (angle < thresholdDeg) sharp++;
        }
        return sharp;
    }

    int CountTrueCornerAngles(Point[] contour, double minDeg, double maxDeg, double sharpnessThresholdDeg = 25)
    {
        int count = 0;
        List<double> sharpAngles = new List<double>();

        int len = contour.Length;
        for (int i = 0; i < len; i++)
        {
            Point a = contour[(i - 1 + len) % len];
            Point b = contour[i];
            Point c = contour[(i + 1) % len];

            double angle = GetAngleDeg(a, b, c);

            // Check if this point is a "corner" (sharp change from 180°)
            double deviationFromFlat = Math.Abs(180 - angle);
            if (deviationFromFlat > sharpnessThresholdDeg)
            {
                if (angle >= minDeg && angle <= maxDeg)
                {
                    count++;
                }
                sharpAngles.Add(angle); // For debugging or visualization
            }
        }

        return count;
    }

    double GetAngleDeg(Point a, Point b, Point c)
    {
        var ab = new Point2f(a.X - b.X, a.Y - b.Y);
        var cb = new Point2f(c.X - b.X, c.Y - b.Y);

        double dot = ab.X * cb.X + ab.Y * cb.Y;
        double cross = ab.X * cb.Y - ab.Y * cb.X;

        return Math.Abs(Math.Atan2(cross, dot) * (180.0 / Math.PI));
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

    public static void SaveToResources(Mat binaryMask, string filename = "binary_mask.jpg")
    {
        Mat converted = new Mat();

        // Get StreamingAssets path
        string folderPath = Path.Combine(Application.dataPath, "Resources");

        // Make sure folder exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Full file path
        string filePath = Path.Combine(folderPath, filename);

        Texture2D TextureFromMat(Mat mat)
        {
            Texture2D texture = new Texture2D(mat.Width, mat.Height, TextureFormat.R8, false);
            texture.LoadRawTextureData(mat.Data, mat.Width * mat.Height);
            texture.Apply();
            return texture;
        }

        // Then save
        Texture2D tex = TextureFromMat(binaryMask);
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);



    }
}