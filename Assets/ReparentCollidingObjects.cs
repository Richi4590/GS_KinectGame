using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReparentCollidingObjects : MonoBehaviour
{
    private HashSet<GameObject> attachedObjects = new HashSet<GameObject>();
    public float checkUnparentAfterNSeconds = 0.1f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<MovableObject>(out MovableObject movObjComp))
        {
            if (movObjComp.IsMovableObject && !attachedObjects.Contains(movObjComp.gameObject))
            {
                movObjComp.transform.SetParent(this.transform);
                attachedObjects.Add(movObjComp.gameObject);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<MovableObject>(out MovableObject movObjComp))
        {
            if (movObjComp.IsMovableObject && attachedObjects.Contains(movObjComp.gameObject))
            {
                StartCoroutine(DelayedUnparent(movObjComp.gameObject));
            }
        }
    }

    private IEnumerator DelayedUnparent(GameObject obj)
    {
        yield return new WaitForSeconds(checkUnparentAfterNSeconds); // Delay to check if still colliding

        // Check if the object is still inside the collider after the delay
        if (!IsObjectInsideCollider(obj))
        {
            // If the object is no longer colliding, unparent it
            obj.transform.SetParent(GameObject.Find("SpawnedObjectsRoot").transform);
            attachedObjects.Remove(obj); // Remove it from tracking
        }
        // If it's still inside, do nothing
    }

    private bool IsObjectInsideCollider(GameObject obj)
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        if (collider == null) return false;

        // Check if the object is still inside the collider area
        return collider.OverlapPoint(obj.transform.position);
    }
}
