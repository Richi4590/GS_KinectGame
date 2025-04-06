#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class OnSceneClickDisplayWorldPos : Editor
{
    static OnSceneClickDisplayWorldPos()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneview)
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            // Check for 3D objects first
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                GameObject hitObject = hitInfo.collider.gameObject;
                Debug.Log(hitObject.name + ": World Position " + hitObject.transform.position);
            }
            else
            {
                // If no hit, check for 2D objects
                RaycastHit2D hit2DInfo = Physics2D.Raycast(ray.origin, ray.direction);
                if (hit2DInfo.collider != null)
                {
                    GameObject hitObject2D = hit2DInfo.collider.gameObject;
                    Debug.Log(hitObject2D.name + ": World Position " + hitObject2D.transform.position);
                }
            }
        }

        // Force the SceneView to update
        //sceneview.Repaint();
    }
}
#endif