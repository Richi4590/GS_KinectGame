using UnityEngine;

public class WindBlower : MonoBehaviour
{
    [Header("Wind Settings")]
    public LayerMask affectedLayers;
    public string targetCustomTag = "WindTarget";
    public float forceStrength = 10f;
    public float effectRadius = 10f;
    public float coneAngle = 30f; // Angle of the cone in degrees
    public float boxWidth = 5f; // Box dimensions
    public float boxHeight = 5f;
    public bool isConeShape = false; // Switch between Box or Cone wind
    public Vector3 direction = Vector3.zero; // Additional direction to modify the wind direction

    void FixedUpdate()
    {
        // Calculate wind direction, considering object rotation and additional direction
        Vector3 windDirection = transform.forward + direction;

        // Normalize the wind direction to ensure consistent application of force
        windDirection.Normalize();

        Collider[] colliders;

        if (isConeShape)
        {
            // Only consider objects within the cone's radius
            colliders = Physics.OverlapSphere(transform.position, effectRadius, affectedLayers);

            foreach (Collider col in colliders)
            {
                if (!col.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(targetCustomTag))
                    continue;

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.gameObject == gameObject) continue;

                Vector3 directionToObject = col.transform.position - transform.position;
                float distance = directionToObject.magnitude;

                if (distance > effectRadius) continue; // Skip if outside the radius

                // Check if the object is within the cone's angle (angle between wind and object)
                float angleToWind = Vector3.Angle(directionToObject, windDirection);
                if (angleToWind > coneAngle) continue; // Skip if outside the cone

                // Apply force with exponential falloff
                float scaledForce = forceStrength * Mathf.Exp(-distance / effectRadius);
                rb.AddForce(windDirection * scaledForce, ForceMode.Force);
            }
        }
        else
        {
            // For box, we use a box overlap.
            colliders = Physics.OverlapBox(transform.position, new Vector3(boxWidth, boxHeight, effectRadius) * 0.5f, transform.rotation, affectedLayers);

            foreach (Collider col in colliders)
            {
                if (!col.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(targetCustomTag))
                    continue;

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.gameObject == gameObject) continue;

                Vector3 directionToObject = col.transform.position - transform.position;
                float distance = directionToObject.magnitude;

                // Apply force with exponential falloff
                float scaledForce = forceStrength * Mathf.Exp(-distance / effectRadius);
                rb.AddForce(windDirection * scaledForce, ForceMode.Force);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // Calculate wind direction considering object rotation and added direction
        Vector3 windDirection = transform.forward + direction;
        windDirection.Normalize();  // Make sure it's normalized

        Vector3 aStart = transform.position;
        Vector3 aEnd = aStart + windDirection * 2f;
        DrawArrow(aStart, aEnd, 20f, 0.4f);

        if (isConeShape)
        {
            // Draw a cone-shaped Gizmo
            Gizmos.DrawWireSphere(transform.position, effectRadius);
            Vector3 right = Quaternion.Euler(0, coneAngle, 0) * windDirection.normalized * effectRadius;
            Vector3 left = Quaternion.Euler(0, -coneAngle, 0) * windDirection.normalized * effectRadius;

            Gizmos.DrawLine(transform.position, transform.position + right);
            Gizmos.DrawLine(transform.position, transform.position + left);
        }
        else
        {
            // Draw a box-shaped Gizmo
            DrawRotatedWireCube(transform.position, new Vector3(boxWidth, boxHeight, effectRadius), transform.rotation);
        }

        Collider[] colliders;

        if (isConeShape)
        {
            colliders = Physics.OverlapSphere(transform.position, effectRadius, affectedLayers);
        }
        else
        {
            colliders = Physics.OverlapBox(transform.position, new Vector3(boxWidth, boxHeight, effectRadius), transform.rotation, affectedLayers);
        }

        foreach (Collider col in colliders)
        {
            if (!col.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(targetCustomTag))
                continue;

            Vector3 direction = (col.transform.position - transform.position).normalized;
            Vector3 arrowStart = col.transform.position;
            Vector3 arrowEnd = arrowStart + direction * 2f;

            DrawArrow(arrowStart, arrowEnd, 20f, 0.2f);
        }
    }

    // Manually draw a rotated wire cube using Gizmos
    void DrawRotatedWireCube(Vector3 position, Vector3 size, Quaternion rotation)
    {
        // Calculate the eight corners of the cube
        Vector3 halfSize = size / 2;
        Vector3[] vertices = new Vector3[8];
        vertices[0] = position + rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        vertices[1] = position + rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[2] = position + rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[3] = position + rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[4] = position + rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        vertices[5] = position + rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[6] = position + rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[7] = position + rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z);

        // Connect the vertices to form the cube's edges
        Gizmos.DrawLine(vertices[0], vertices[1]);
        Gizmos.DrawLine(vertices[1], vertices[2]);
        Gizmos.DrawLine(vertices[2], vertices[3]);
        Gizmos.DrawLine(vertices[3], vertices[0]);

        Gizmos.DrawLine(vertices[4], vertices[5]);
        Gizmos.DrawLine(vertices[5], vertices[6]);
        Gizmos.DrawLine(vertices[6], vertices[7]);
        Gizmos.DrawLine(vertices[7], vertices[4]);

        Gizmos.DrawLine(vertices[0], vertices[4]);
        Gizmos.DrawLine(vertices[1], vertices[5]);
        Gizmos.DrawLine(vertices[2], vertices[6]);
        Gizmos.DrawLine(vertices[3], vertices[7]);
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