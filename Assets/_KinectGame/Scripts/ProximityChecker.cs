using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ProximityChecker : MonoBehaviour
{
    public float checkInterval = 0.5f;
    public float detectionRadius = 5f;
    public LayerMask detectionLayer;

    [Header("Events")]
    public UnityEvent OnObjectsNearby;
    public UnityEvent OnNoObjectsNearby;

    private Coroutine checkCoroutine;
    private bool previousState = false;

    void Start()
    {
        checkCoroutine = StartCoroutine(CheckProximityRoutine());
    }

    IEnumerator CheckProximityRoutine()
    {
        while (true)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
            bool objectsDetected = nearbyColliders.Length > 0;

            if (objectsDetected && !previousState)
            {
                OnObjectsNearby?.Invoke();
            }
            else if (!objectsDetected && previousState)
            {
                OnNoObjectsNearby?.Invoke();
            }

            previousState = objectsDetected;

            yield return new WaitForSeconds(checkInterval);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
