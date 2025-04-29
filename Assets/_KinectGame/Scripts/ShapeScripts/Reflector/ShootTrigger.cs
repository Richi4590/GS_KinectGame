using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootTrigger : MonoBehaviour
{
    public GameObject projectilePrefab; // The projectile to be fired
    public Transform projectileSpawnPoint; // Where the projectile spawns
    public float projectileSpeed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogError("ProjectilePrefab or ProjectileSpawnPoint is not assigned!");
            return;
        }

        // Create the projectile
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        projectile.SetActive(false);

        // Calculate velocity towards the target
        Vector3 directionToTarget = projectileSpawnPoint.forward.normalized;

        // Normalize the modified direction and scale by speed
        Vector3 velocity = directionToTarget.normalized * projectileSpeed;

        projectile.GetComponent<Projectile>().ShootProjectile(this.gameObject, null, velocity);
    }
}
