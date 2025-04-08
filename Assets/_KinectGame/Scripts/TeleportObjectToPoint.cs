using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [SerializeField] private Transform teleportDestination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && teleportDestination != null)
        {
            other.transform.position = teleportDestination.position;
        }
    }
}

