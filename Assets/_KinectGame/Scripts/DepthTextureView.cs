using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;
using Windows.Kinect;
using Rect = UnityEngine.Rect;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

public class DepthObjectDetector : MonoBehaviour
{
    public GameObject DepthSourceManager;
    public GameObject ObjectPrefab;
    public GameObject DebugObjectCenterPrefab;
    public Transform spawnedObjectsParentRoot;
    [Min(0)] public int MinDepth = 500;
    [Min(0)] public int MaxDepth = 1000;
    [Min(0)] public int minBlobSizesToRemove = 10;
    [Min(0)] public int dilationIterations = 2;
    [Min(0)] public int erosionIterations = 2;
    [Range(0, 100)] public int borderThickness = 0;

    public Vector3 generated2DMeshObjScale = Vector3.one;
    [Min(0)] public float simplificationTolerance = 0.02f;
    private Vector3 generatedMeshRotationOffset = Vector3.zero;

    public bool flipSpriteX = false;
    public bool flipSpriteY = false;

    public int framesToWait = 10;
    public bool showDebugCenterPos = false;

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
            ClearSpawnedObjects();
            ProcessDepthData(_DepthManager.GetData());
            //InstantiateObjectsFromBounds(objectBounds);
            InstantiateObjectsWithMesh();
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

    void ProcessDepthData(ushort[] depthData)
    {
        if (depthData == null) return;

        // Convert depth data to OpenCV Mat
        Mat depthMat = new Mat(_DepthHeight, _DepthWidth, MatType.CV_16UC1, depthData);
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
        gameObject.GetComponent<Renderer>().material.mainTexture = _BinaryMaskTexture;
    }

    void InstantiateObjectsWithMesh()
    {
        // Find contours in the binary image
        HierarchyIndex[] hierarchy;
        Point[][] contours;
        Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        objectBounds.Clear();

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > minBlobSizesToRemove) // Filter out small objects
            {
                //BOUNDS GEN

                // Convert 2D depth positions to world positions
                Vector2[] worldPoints = new Vector2[contour.Length];
                for (int i = 0; i < contour.Length; i++)
                {
                    Vector2 viewportPoint = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));
                    worldPoints[i] = new Vector3(viewportPoint.x, viewportPoint.y, 0); // Set Z to 0 initially
                }

                worldPoints = SimplifyPolygon(worldPoints, simplificationTolerance);

                // Get bounding box (optional, for debugging)
                OpenCvSharp.Rect boundingBox = Cv2.BoundingRect(contour);
                objectBounds.Add(new Rect(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height));

                // Compute center of the bounding box in world space
                Vector2 gameObjectWorldPosition = DepthToViewport(new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2));

                // Create a new GameObject
                GameObject obj = Instantiate(ObjectPrefab, spawnedObjectsParentRoot.transform.InverseTransformPoint(gameObjectWorldPosition), ObjectPrefab.transform.rotation, spawnedObjectsParentRoot);
                obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y, 0);
                obj.transform.localScale = generated2DMeshObjScale;
                _SpawnedObjects.Add(obj);

                //DEBUG CENTROID OBJ GEN
                if (showDebugCenterPos)
                {
                    Vector2 centerWorldPos = new Vector3(gameObjectWorldPosition.x, gameObjectWorldPosition.y);
                    GameObject centerObj = Instantiate(DebugObjectCenterPrefab, centerWorldPos, ObjectPrefab.transform.rotation, spawnedObjectsParentRoot);
                    centerObj.transform.localPosition = new Vector3(centerObj.transform.localPosition.x, centerObj.transform.localPosition.y, -0.2f);
                    centerObj.transform.localScale *= 5;
                    _SpawnedObjects.Add(centerObj);
                }


                if (!obj.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
                    spriteRenderer = obj.AddComponent<SpriteRenderer>();

                //SPRITE GEN
                spriteRenderer.sprite = CreateSpriteFromContour(worldPoints);

                //COLLIDER GEN
                if (!obj.TryGetComponent<PolygonCollider2D>(out PolygonCollider2D polygonCollider))
                    polygonCollider = obj.AddComponent<PolygonCollider2D>();


                //colliderVertices = SimplifyPolygon(colliderVertices, simplificationTolerance);

                Vector2 centroid = new Vector2(obj.transform.localPosition.x, obj.transform.localPosition.y);

                // Offset the collider points to align with the sprite
                for (int i = 0; i < worldPoints.Length; i++)
                {
                    worldPoints[i] -= centroid; // Center it to match sprite positioning
                    worldPoints[i] /= generated2DMeshObjScale.x;  // Scale correction
                }

                // Apply to collider
                polygonCollider.SetPath(0, worldPoints);
            }
        }
    }

    void InstantiateObjectsWith3DMesh()
    {
        // Find contours in the binary image
        HierarchyIndex[] hierarchy;
        Point[][] contours;
        Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        objectBounds.Clear();

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > minBlobSizesToRemove) // Filter out small objects
            {
                // Convert 2D depth positions to world positions
                Vector3[] worldPoints = new Vector3[contour.Length];
                for (int i = 0; i < contour.Length; i++)
                {
                    Vector2 viewportPoint = DepthToViewport(new Vector2(contour[i].X, contour[i].Y));

                    // Flip Y-axis to match Unity�s coordinate system
                    viewportPoint.y = 1.0f - viewportPoint.y;

                    worldPoints[i] = new Vector3(viewportPoint.x, viewportPoint.y, 0); // Set Z to 0 initially
                }

                // Get bounding box (optional, for debugging)
                OpenCvSharp.Rect boundingBox = Cv2.BoundingRect(contour);
                objectBounds.Add(new Rect(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height));

                // Compute center of the bounding box in world space
                Vector3 gameObjectWorldPosition = DepthToViewport(new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2));
                gameObjectWorldPosition.z = 0.0f;

                // Create a new GameObject
                GameObject obj = Instantiate(ObjectPrefab, gameObjectWorldPosition, ObjectPrefab.transform.rotation, spawnedObjectsParentRoot);
                obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y, 0);
                _SpawnedObjects.Add(obj);

                if (showDebugCenterPos)
                {
                    Vector3 centerWorldPos = new Vector3(gameObjectWorldPosition.x, gameObjectWorldPosition.y, gameObjectWorldPosition.z - 0.1f);
                    GameObject centerObj = Instantiate(ObjectPrefab, centerWorldPos, ObjectPrefab.transform.rotation, spawnedObjectsParentRoot);
                    centerObj.transform.localPosition = new Vector3(centerObj.transform.localPosition.x, centerObj.transform.localPosition.y, 0);
                    centerObj.transform.localScale /= 2;

                    centerObj.GetComponent<MeshRenderer>().material.color = Color.red;
                    _SpawnedObjects.Add(centerObj);
                }

                // Ensure it has a MeshFilter & MeshRenderer
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = obj.AddComponent<MeshFilter>();

                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = obj.AddComponent<MeshRenderer>();

                // Generate mesh from contour
                Mesh mesh = TriangulateContour(worldPoints);

                if (mesh != null)
                {
                    // Apply rotation to mesh vertices (rotate mesh around the GameObject's position)
                    Quaternion rotation = Quaternion.Euler(generatedMeshRotationOffset.x, generatedMeshRotationOffset.y, generatedMeshRotationOffset.z); // Rotation angles

                    // Use the GameObject's position as the bounding box center and mesh center
                    Vector3 boundingBoxCenter = gameObjectWorldPosition;

                    // Mesh center is the same as the GameObject's position but with a flipped Y component (adjust as necessary)
                    Vector3 meshCenter = gameObjectWorldPosition;
                    meshCenter.y = -meshCenter.y;  // This handles any coordinate system offsets

                    // Translate mesh vertices to align with the mesh center
                    for (int i = 0; i < worldPoints.Length; i++)
                    {
                        worldPoints[i] -= meshCenter;  // Move vertices to mesh center
                        worldPoints[i] += boundingBoxCenter;  // Move mesh center to bounding box center
                    }

                    // Apply rotation around the GameObject's position
                    for (int i = 0; i < worldPoints.Length; i++)
                    {
                        worldPoints[i] = rotation * (worldPoints[i] - boundingBoxCenter);  // Rotate around GameObject position
                    }

                    // Set the rotated vertices to the mesh
                    mesh.vertices = worldPoints;

                    // Recalculate bounds and normals
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    // Assign the mesh to the MeshFilter
                    meshFilter.mesh = mesh;

                    // Create a new PolygonCollider2D (assuming your mesh is 2D and aligned with the X-Z plane)
                    PolygonCollider2D polygonCollider = obj.AddComponent<PolygonCollider2D>();

                    // Extract the 2D vertices for the collider (ignoring the Y component)
                    Vector2[] colliderVertices = new Vector2[worldPoints.Length];
                    for (int i = 0; i < worldPoints.Length; i++)
                    {
                        // Use X and Z coordinates for the 2D collider
                        colliderVertices[i] = new Vector2(worldPoints[i].x, worldPoints[i].z);
                    }

                    // Set the collider points
                    polygonCollider.SetPath(0, colliderVertices);
                }
                else
                {
                    _SpawnedObjects.Remove(obj);
                    Destroy(obj);
                }
            }
        }

        Debug.Log("");
    }

    Mesh TriangulateContour(Vector3[] points)
    {

        if (points.Length < 3) return null; // A mesh needs at least 3 points
        
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>(points);
        List<int> triangles = new List<int>();

        // Convert Vector3 points to OpenCV Points (2D)
        Point[] cvPoints = points.Select(p => new Point((int)p.x, (int)p.y)).ToArray();

        // Find convex hull
        Mat hullIndicesMat = new Mat();
        Cv2.ConvexHull(InputArray.Create(cvPoints), hullIndicesMat, returnPoints: false);

        // Extract indices from Mat
        int[] hullIndices = new int[hullIndicesMat.Rows]; // Allocate space
        hullIndicesMat.GetArray(0, 0, hullIndices); // Copy data

        // Generate triangles from convex hull
        for (int i = 1; i < hullIndices.Length - 1; i++)
        {
            triangles.Add(hullIndices[0]);
            triangles.Add(hullIndices[i]);
            triangles.Add(hullIndices[i + 1]);
        }

        // Assign data to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Vector2[] SimplifyPolygon(Vector2[] points, float tolerance)
    {
        if (points.Length < 3) return points; // No need to simplify if less than 3 points
        List<Vector2> simplified = RamerDouglasPeucker(points, tolerance);
        return simplified.ToArray();
    }

    private static List<Vector2> RamerDouglasPeucker(Vector2[] points, float epsilon)
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

    Sprite CreateSpriteFromContour(Vector2[] worldPoints)
    {
        // Convert the 3D points to 2D points by ignoring the Z component
        Vector2[] contour2D = new Vector2[worldPoints.Length];
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;


        // Convert world points to 2D and calculate the bounding box
        for (int i = 0; i < worldPoints.Length; i++)
        {
            contour2D[i] = new Vector2(worldPoints[i].x, worldPoints[i].y);

            if (flipSpriteX)
                contour2D[i].x *= -1;

            if (flipSpriteY)
                contour2D[i].y *= -1;

            // Update min/max bounds
            minX = Mathf.Min(minX, contour2D[i].x);
            maxX = Mathf.Max(maxX, contour2D[i].x);
            minY = Mathf.Min(minY, contour2D[i].y);
            maxY = Mathf.Max(maxY, contour2D[i].y);
        }

        // Ensure the bounds are valid and both width and height are greater than 0
        float width = Mathf.Abs(maxX - minX);
        float height = Mathf.Abs(maxY - minY);

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Invalid contour size: width or height is less than or equal to 0.");
            return null;  // Return null if the contour is invalid
        }

        // Create a texture with a size large enough to fit the contour
        Texture2D texture = new Texture2D(Mathf.CeilToInt(width), Mathf.CeilToInt(height));

        // Fill the texture with transparent color
        Color32 transparentColor = new Color32(0, 0, 0, 0);
        Color32 whiteColor = new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparentColor;
        }
        texture.SetPixels32(pixels);

        // Offset the contour so it fits within the texture bounds
        Vector2 offset = new Vector2(-minX, -minY); // Ensure the contour fits within positive coordinates

        // Apply the offset to contour points
        for (int i = 0; i < contour2D.Length; i++)
        {
            contour2D[i] += offset;
        }

        // Ensure the texture is large enough to hold the contour
        if (texture.width < Mathf.CeilToInt(width) || texture.height < Mathf.CeilToInt(height))
        {
            texture.Reinitialize(Mathf.CeilToInt(width), Mathf.CeilToInt(height));
        }

        // Draw the contour onto the texture using a custom DrawLine function
        for (int i = 0; i < contour2D.Length - 1; i++)
        {
            Vector2 start = contour2D[i];
            Vector2 end = contour2D[i + 1];

            DrawLine(texture, start, end, whiteColor);
        }

        // Close the loop by connecting the last point to the first point
        DrawLine(texture, contour2D[contour2D.Length - 1], contour2D[0], whiteColor);

        // Apply the changes to the texture
        texture.Apply();

        // Create and return a sprite from the texture
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color32 color)
    {
        // Convert the start and end points to integers (rounding the coordinates)
        int startX = Mathf.RoundToInt(start.x);
        int startY = Mathf.RoundToInt(start.y);
        int endX = Mathf.RoundToInt(end.x);
        int endY = Mathf.RoundToInt(end.y);

        // Ensure that the start and end points are within the texture bounds
        startX = Mathf.Clamp(startX, 0, texture.width - 1);
        startY = Mathf.Clamp(startY, 0, texture.height - 1);
        endX = Mathf.Clamp(endX, 0, texture.width - 1);
        endY = Mathf.Clamp(endY, 0, texture.height - 1);

        // Bresenham's Line Algorithm using integer-based coordinates
        int dx = Mathf.Abs(endX - startX);
        int dy = Mathf.Abs(endY - startY);
        int sx = startX < endX ? 1 : -1;
        int sy = startY < endY ? 1 : -1;
        int err = dx - dy;

        // Loop from the start to the end point
        int currentX = startX;
        int currentY = startY;

        // Continue the loop until we reach the end point
        for (int i = 0; i <= Mathf.Max(dx, dy); i++)
        {
            // Set the pixel at the current position
            texture.SetPixel(currentX, currentY, color);

            // Calculate error twice to avoid floating-point precision issues
            int e2 = err * 2;

            if (e2 > -dy)
            {
                err -= dy;
                currentX += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                currentY += sy;
            }

            // Check if we've reached the endpoint
            if (currentX == endX && currentY == endY)
                break;
        }
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