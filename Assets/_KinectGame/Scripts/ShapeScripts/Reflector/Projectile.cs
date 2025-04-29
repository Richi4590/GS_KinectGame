using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject shooter = null; //  The object that fired the projectile (e.g., a player or enemy)
    public GameObject target = null; // The object the projectile should follow (if heat-seeking is enabled).

    public bool heatSeeking = false; // Toggles the heat-seeking behavior. When `true`, the projectile adjusts its trajectory toward the target.
    public bool reflected = false; // Indicates whether the projectile has been reflected (e.g., by a lightsaber).
    public bool debugRotation = false; //  Enables debugging by allowing manual adjustment of the projectile's rotation during runtime.

    public Vector3 rotationOffset = new Vector3(90, 0, 0); //  A rotation offset applied to the projectile's orientation for customization (e.g., aligning with models).
    public float reflectionForceMultiplier = 1.0f; // Multiplier for reflected laser speed
    public float destroyProjectileAfterNSeconds = 10f;
    public string tagOfObjectToReflectFrom = "Reflector";

    public List<GameObject> sparksPrefabs; // A list of spark effects to instantiate upon collision.
    public List<AudioClip> projectileDeflectionSFX; // A list of sound effects for when the projectile is deflected.

    private Rigidbody rb;
    private Collider coll;
    private Vector3 currentVelocity = Vector3.zero; // Stores the projectile's current velocity for heat-seeking and reflection calculations.
    private bool applyFinalVelocity = false; // A flag to apply the reflected velocity in the next physics update.


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    private void Update()
    {
        if (debugRotation)
        {
            ChangeTarget(target);
        }
    }

    private void FixedUpdate()
    {
        if (applyFinalVelocity)
        {
            rb.velocity = currentVelocity;
            applyFinalVelocity = false; // Reset after applying
        }
    }

    public void ShootProjectile(GameObject shooter, GameObject target, Vector3 velocity)
    {
        this.shooter = shooter;
        ChangeTarget(target);

        //transform.LookAt(newTarget.transform.position, Vector3.up);
        transform.rotation = Quaternion.LookRotation(velocity.normalized);
        transform.rotation *= Quaternion.Euler(rotationOffset.x, rotationOffset.y, rotationOffset.z);
        Debug.DrawRay(transform.position, velocity, Color.yellow, 2);

        currentVelocity = velocity;
        rb.velocity = currentVelocity;
        Destroy(this.gameObject, destroyProjectileAfterNSeconds);
        gameObject.SetActive(true);
    }

    public void ChangeTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    public void SetReflected(Vector3 newVelocity, bool reflected)
    {
        currentVelocity = newVelocity;
        applyFinalVelocity = true;
        this.reflected = reflected;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent<CustomTag>(out CustomTag t) || !t.Tags.Contains(tagOfObjectToReflectFrom))
        {
            // Get the contact point and normal of the collision
            ContactPoint contact = collision.GetContact(0);

            InstantiateSpark(contact);
            
            Destroy(this.gameObject);
            return;
        }

        //Reflect!
        Reflect(collision);
    }

    public void Reflect(Collision surfaceCollision)
    {
        LayerMask layersToIgnore = LayerMask.GetMask("Laser", "Player");
        rb.excludeLayers = layersToIgnore;
        coll.excludeLayers = layersToIgnore;

        PlayRandomDeflectionSound(surfaceCollision.collider.GetComponent<AudioSource>());
        InstantiateSpark(surfaceCollision.GetContact(0));

        // Prevent angular motion
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Capture the velocity before Unity modifies it
        Vector3 incomingVelocity = currentVelocity;
        float incomingSpeed = incomingVelocity.magnitude; // Preserve the speed

        // Get the contact point and normal
        ContactPoint contact = surfaceCollision.GetContact(0);
        Vector3 hitNormal = contact.normal;

        // Calculate the reflection direction
        Vector3 reflectedDirection = Vector3.Reflect(incomingVelocity.normalized, hitNormal);

        // Align the projectile's rotation with the reflected velocity
        Quaternion targetRotation = Quaternion.LookRotation(reflectedDirection);
        transform.rotation = targetRotation * Quaternion.Euler(90, 0, 0);

        // Calculate the new velocity while preserving speed
        Vector3 velocityAfterReflection = reflectedDirection * incomingSpeed * reflectionForceMultiplier;

        // Mark the laser as reflected
        SetReflected(velocityAfterReflection, true);
        // Debugging: Visualize the reflection
        Debug.DrawRay(contact.point, reflectedDirection, Color.blue, 2.0f);

    }

    private void PlayRandomDeflectionSound(AudioSource deflectionAudioSource)
    {
        deflectionAudioSource.PlayOneShot(projectileDeflectionSFX[Random.Range(0, projectileDeflectionSFX.Count)]);
    }

    private void InstantiateSpark(ContactPoint contact)
    {
        GameObject sparkSFX = Instantiate(sparksPrefabs[Random.Range(0, sparksPrefabs.Count)], contact.point, Quaternion.LookRotation(contact.normal));

        float longestWaitingTime = 0;

        int timesSmaller = 10;
        sparkSFX.transform.localScale = sparkSFX.transform.localScale / timesSmaller;

        longestWaitingTime = sparkSFX.GetComponent<ParticleSystem>().main.duration;

        //Make all sub sfx the same size
        for (int i = 0; i < sparkSFX.transform.childCount; i++)
        {
            GameObject child = sparkSFX.transform.GetChild(i).gameObject;
            child.transform.localScale = sparkSFX.transform.localScale / timesSmaller;

            if (child.GetComponent<ParticleSystem>().main.duration > longestWaitingTime)
                longestWaitingTime = child.GetComponent<ParticleSystem>().main.duration;
        }

        //Debug.Log("Longest Particle duration of: " + longestWaitingTime);

        Destroy(sparkSFX, longestWaitingTime);
    }
}