using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TriggerEventsBlockingBridge : MonoBehaviour
{
    public BlockingBridge walkArea;
    private HashSet<GameObject> bridgesInside = new HashSet<GameObject>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            int newLayer = LayerMask.NameToLayer("WalkableBridge"); // Get the layer ID by name
            if (newLayer != -1) // Check if the layer is valid
                collision.gameObject.layer = newLayer;

            collision.gameObject.GetComponent<Collider2D>().isTrigger = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            bridgesInside.Add(collision.gameObject);
            walkArea.UpdateBlockerCollider(bridgesInside);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("GeneratedMeshObject"))
        {
            int newLayer = LayerMask.NameToLayer("Default"); // Get the layer ID by name
            if (newLayer != -1) // Check if the layer is valid
                collision.gameObject.layer = newLayer;

            collision.gameObject.GetComponent<Collider2D>().isTrigger = false;
            bridgesInside.Remove(collision.gameObject);
            walkArea.UpdateBlockerCollider(bridgesInside);
        }
    }
}
