#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CameraFrustumVisualizer
{
    static CameraFrustumVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Handles.color = new Color(0f, 1f, 1f, 0.5f);

        Vector3[] nearCorners = new Vector3[4];
        Vector3[] farCorners = new Vector3[4];

        Transform camTransform = cam.transform;

        if (!cam.orthographic)
        {
            // Perspective: Use Camera.CalculateFrustumCorners
            cam.CalculateFrustumCorners(
                new Rect(0, 0, 1, 1),
                cam.nearClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                nearCorners
            );

            cam.CalculateFrustumCorners(
                new Rect(0, 0, 1, 1),
                cam.farClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                farCorners
            );
        }
        else
        {
            // Orthographic: manually define rectangle
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            nearCorners[0] = new Vector3(-halfWidth, halfHeight, 0); // top-left
            nearCorners[1] = new Vector3(halfWidth, halfHeight, 0); // top-right
            nearCorners[2] = new Vector3(halfWidth, -halfHeight, 0); // bottom-right
            nearCorners[3] = new Vector3(-halfWidth, -halfHeight, 0); // bottom-left

            float depth = cam.farClipPlane - cam.nearClipPlane;

            for (int i = 0; i < 4; i++)
            {
                farCorners[i] = nearCorners[i] + Vector3.forward * depth;
            }
        }

        // Transform to world space
        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = camTransform.TransformPoint(nearCorners[i]);
            farCorners[i] = camTransform.TransformPoint(farCorners[i]);
        }

        // Draw edges
        for (int i = 0; i < 4; i++)
        {
            Handles.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4]);
            Handles.DrawLine(farCorners[i], farCorners[(i + 1) % 4]);
            Handles.DrawLine(nearCorners[i], farCorners[i]);
        }
    }
}
#endif