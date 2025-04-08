using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class FallingInPitBridge3D : MonoBehaviour
{
    [Header("Blocking Area Settings")]
    public Color gizmoColor = new Color(1, 0, 0, 0.3f);

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    } 

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z));
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            int newLayer = LayerMask.NameToLayer("WalkableBridge"); // Get the layer ID by name
            if (newLayer != -1) // Check if the layer is valid
                collision.gameObject.layer = newLayer;

            MeshCollider meshColl = collision.gameObject.GetComponent<MeshCollider>();

            meshColl.convex = true;
            meshColl.isTrigger = true;
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        /*
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            bridgesInside.Add(collision.gameObject);
            walkArea.UpdateBlockerCollider(bridgesInside);
        }*/

    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            int newLayer = LayerMask.NameToLayer("Default"); // Get the layer ID by name
            if (newLayer != -1) // Check if the layer is valid
                collision.gameObject.layer = newLayer;

            MeshCollider meshColl = collision.gameObject.GetComponent<MeshCollider>();

            meshColl.convex = false;
            meshColl.isTrigger = false;
        }
    }
}