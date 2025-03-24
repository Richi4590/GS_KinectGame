using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;
using Windows.Kinect;
using Rect = UnityEngine.Rect;

public class DepthObjectDetector : MonoBehaviour
{
    public GameObject DepthSourceManager;
    public GameObject ObjectPrefab;
    public Transform spawnedObjectsParentRoot;
    public ushort MinDepth = 500;
    public ushort MaxDepth = 1000;
    public float SpawnedObjectsZOffset = 0.0f;
    public bool useCustomScaling = false;
    public Vector3 customScaling = Vector3.one;

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
    }

    void Update()
    {
        ClearSpawnedObjects();
        ProcessDepthData(_DepthManager.GetData());
        InstantiateObjectsFromBounds(objectBounds);
    }

    void ProcessDepthData(ushort[] depthData)
    {
        if (depthData == null) return;

        // Use OpenCV thresholding instead of manual pixel assignment
        Mat depthMat = new Mat(_DepthHeight, _DepthWidth, MatType.CV_16UC1, depthData);
        Cv2.InRange(depthMat, MinDepth, MaxDepth, _BinaryImage);

        // Connected components
        int numComponents = Cv2.ConnectedComponentsWithStats(_BinaryImage, _Labels, _Stats, _Centroids);

        for (int i = 1; i < numComponents; i++)  // Ignore background (i=0)
        {
            int area = _Stats.At<int>(i, 4);
            if (area > 10)  // Ignore very small blobs
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
        foreach (var bound in bounds)
        {
            Vector3 position = DepthToViewport(new Vector2(bound.x + bound.width / 2, bound.y + bound.height / 2));
            Vector3 size = useCustomScaling ? customScaling : DepthToViewportSize(new Vector2(bound.width, bound.height));

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
        Vector3 worldSize = _MainCamera.ViewportToWorldPoint(new Vector3(depthSize.x / _DepthWidth, depthSize.y / _DepthHeight, 0));
        return new Vector3(worldSize.x * 2, worldSize.y * 2, 1);
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