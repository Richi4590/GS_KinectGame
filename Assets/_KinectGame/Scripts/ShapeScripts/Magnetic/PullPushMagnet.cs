using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullPushMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    public LayerMask affectedLayers;
    public string targetCustomTag = "MagnetTarget";
    public float forceStrength = 10f;
    public bool isPulling = true; // true = pull, false = push
    public float effectRadius = 10f;

    void FixedUpdate()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius, affectedLayers);

        foreach (Collider col in colliders)
        {
            if (!col.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(targetCustomTag))
                continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb == null || rb.gameObject == gameObject) continue;

            Vector3 direction = (col.transform.position - transform.position).normalized;
            Vector3 forceDirection = isPulling ? -direction : direction;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            float scaledForce = forceStrength * (1f - distance / effectRadius); // Optional falloff

            rb.AddForce(forceDirection * scaledForce, ForceMode.Force);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isPulling ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, effectRadius);

        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius, affectedLayers);
        foreach (Collider col in colliders)
        {
            if (!col.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(targetCustomTag))
                continue;

            Vector3 direction = (col.transform.position - transform.position).normalized;
            Vector3 arrowStart = col.transform.position;
            Vector3 arrowEnd = arrowStart + (isPulling ? -direction : direction) * 2f;

            DrawArrow(arrowStart, arrowEnd, 20f, 0.2f);
        }
    }

    void DrawArrow(Vector3 from, Vector3 to, float arrowHeadAngle, float arrowHeadLength)
    {
        Gizmos.DrawLine(from, to);

        Vector3 direction = (to - from).normalized;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

        Gizmos.DrawLine(to, to + right * arrowHeadLength);
        Gizmos.DrawLine(to, to + left * arrowHeadLength);
    }
}
