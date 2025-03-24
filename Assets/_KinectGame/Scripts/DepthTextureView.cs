using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;
using Windows.Kinect;
using Rect = UnityEngine.Rect;
using System.Collections;

public class DepthObjectDetector : MonoBehaviour
{
    public GameObject DepthSourceManager;
    public GameObject ObjectPrefab;
    public Transform spawnedObjectsParentRoot;
    [Min(0)] public int MinDepth = 500;
    [Min(0)] public int MaxDepth = 1000;
    [Min(0)] public int minBlobSizesToRemove = 10;
    [Min(0)] public int dilationIterations = 2;
    [Min(0)] public int erosionIterations = 2;
    [Range(0, 100)] public int borderThickness = 0;

    public float SpawnedObjectsZOffset = 0.0f;
    public bool useCustomScaling = false;
    public float scaleDivider = 4;
    public int framesToWait = 10;

    private KinectSensor _Sensor;
    private DepthSourceManager _DepthManager;
    private int _DepthWidth = 512;
    private int _DepthHeight = 424;
    private Texture2D _BinaryMaskTexture;
    private List<Rect> objectBounds;
    private List<GameObject> _SpawnedObjects = new List<GameObject>();
    private Camera _MainCamera;
    private Mat _BinaryImage;
    private Mat _Labels;
    private Mat _Stats;
    private Mat _Centroids;

    private bool DEBUG_FloodFill_Mask = false;

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null) _Sensor.Open();

        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
        _BinaryMaskTexture = new Texture2D(_DepthWidth, _DepthHeight);
        _MainCamera = Camera.main;

        // Allocate OpenCV Mats only once
        _BinaryImage = new Mat(_DepthHeight, _DepthWidth, MatType.CV_8UC1);
        _Labels = new Mat();
        _Stats = new Mat();
        _Centroids = new Mat();
        objectBounds = new List<Rect>();

        StartCoroutine(ExecuteEveryNFrames(framesToWait));
    }

    void Update()
    {

    }

    // Coroutine that will execute every N frames
    IEnumerator ExecuteEveryNFrames(int waitForFrames)
    {
        while (true)
        {
            // Wait for N frames
            yield return new WaitForSeconds(1f / ((int)(1.0f / Time.smoothDeltaTime)) * waitForFrames);  
            ClearSpawnedObjects();
            ProcessDepthData(_DepthManager.GetData());
            InstantiateObjectsFromBounds(objectBounds);
        }
    }

    void ProcessDepthData(ushort[] depthData)
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
            if (area > minBlobSizesToRemove)  // Ignore very small blobs
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
        gameObject.GetComponent<Renderer>().material.mainTexture = _BinaryMaskTexture;
    }

    void InstantiateObjectsFromBounds(List<Rect> bounds)
    {
        foreach (Rect bound in bounds)
        {
            Vector3 position = DepthToViewport(new Vector2(bound.x + bound.width / 2, bound.y + bound.height / 2));
            Vector3 size = DepthToViewportSize(new Vector2(bound.width, bound.height));

            GameObject obj = Instantiate(ObjectPrefab, position, ObjectPrefab.transform.rotation, spawnedObjectsParentRoot);
            obj.transform.localScale = size;
            _SpawnedObjects.Add(obj);
        }
    }

    Vector3 DepthToViewport(Vector2 depthPos)
    {
        float x = (depthPos.x / _DepthWidth) * 2 - 1;
        float y = (depthPos.y / _DepthHeight) * 2 - 1;

        x = -x;
        y = -y;

        return _MainCamera.ViewportToWorldPoint(new Vector3((x + 1) / 2, (y + 1) / 2, SpawnedObjectsZOffset));
    }

    Vector3 DepthToViewportSize(Vector2 depthSize)
    {
        // Calculate the world size based on the camera's orthographic settings
        float worldHeight = _MainCamera.orthographicSize;  // Camera height in world units
        float worldWidth = worldHeight * _MainCamera.aspect;    // Camera width based on the aspect ratio

        // Normalize the depth size based on the camera's world size
        float normalizedX = depthSize.x / _DepthWidth;           // Normalized X coordinate (0 to 1)
        float normalizedY = depthSize.y / _DepthHeight;         // Normalized Y coordinate (0 to 1)

        // Calculate the world space size based on normalized coordinates and camera's world space size
        float worldX = normalizedX * worldWidth;                 // World width corresponding to the depth width
        float worldY = normalizedY * worldHeight;                // World height corresponding to the depth height;

        // Return the calculated world size as a Vector3 (Z is set to 0 for 2D)
        return new Vector3(worldX/scaleDivider, 0, worldY/scaleDivider);
    }

    void ClearSpawnedObjects()
    {
        foreach (GameObject obj in _SpawnedObjects) Destroy(obj);
        objectBounds.Clear();
        _SpawnedObjects.Clear();
    }

    void OnApplicationQuit()
    {
        if (_Sensor != null && _Sensor.IsOpen) _Sensor.Close();
        _BinaryImage.Dispose();
        _Labels.Dispose();
        _Stats.Dispose();
        _Centroids.Dispose();
    }
}