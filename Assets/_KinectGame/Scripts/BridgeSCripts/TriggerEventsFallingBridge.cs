using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(BoxCollider2D))]
public class TriggerEventsFallingBridge : MonoBehaviour
{
    public FallingInPitBridge walkArea;
    public float overlapThreshold = 0.5f;
    private HashSet<GameObject> bridgesInside = new HashSet<GameObject>();
    private Collider2D triggerCollider;
    // Start is called before the first frame update
    void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
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


        if (collision.gameObject.tag == "Player")
        {
            if (Utilities.IsColliderInside(collision, triggerCollider, overlapThreshold))
            {
                Vector3 objPosition = collision.gameObject.transform.position;
                StartCoroutine(MoveToTargetScaleAndResetPos(collision.gameObject.transform, new Vector3(triggerCollider.transform.position.x, objPosition.y, objPosition.z), 2f)); // Example duration of 2 seconds


            }
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

    IEnumerator MoveToTargetScaleAndResetPos(Transform obj, Vector3 target, float duration)
    {
        if (obj == null) yield break;

        // Store the original scale
        Vector3 originalScale = obj.localScale;

        // Position the object on the same X-axis
        obj.position = target;

        // Scaling down over time
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float scaleFactor = Mathf.Lerp(originalScale.x, 0.1f, elapsedTime / duration);
            obj.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final scale is approximately 0.1
        obj.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Wait a short moment
        yield return new WaitForSeconds(0.5f);

        // Reset the scale back to original
        obj.localScale = originalScale;

        obj.transform.position = obj.GetComponent<LastValidPosition>().GetLastValidPosition();
    }


}
