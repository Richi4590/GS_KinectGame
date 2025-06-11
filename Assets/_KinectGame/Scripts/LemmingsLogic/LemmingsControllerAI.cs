using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class LemmingsControllerAI : MonoBehaviour
{
    public enum State { IdleWander, Marching }
    public State currentState = State.IdleWander;

    public float wanderRadius = 5f;
    public float moveSpeed = 2f;
    public event UnityAction<LemmingsControllerAI> OnDestroyEvent;
    public event UnityAction<LemmingsControllerAI> OnRespawnEvent;

    public bool LemmingInitialized {get => lemmingInitialized;}

    private List<string> TagsLemmingsHave = new List<string>();
    private List<string> TagsLemmingsShouldReactTo = new List<string>();
    private LayerMask layersLemmingsIgnore;

    private Rigidbody rb;
    private Collider coll;

    private Vector3 spawnCenter;
    private Vector3 wanderTarget;

    private bool lemmingInitialized = false;
    private float wanderTime = 2f;
    private float wanderTimer;
    private float initialMoveSpeed;
    private CustomTag lemmingCustomTags;
    private LemmingsMarchManager manager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        lemmingCustomTags = GetComponent<CustomTag>();
    }

    void Start()
    {
        initialMoveSpeed = moveSpeed;
        spawnCenter = transform.position;
        Wander();
    }

    void Update()
    {
        if (currentState == State.IdleWander)
        {
            Wander();
        }
        else if (currentState == State.Marching)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((layersLemmingsIgnore & (1 << collision.gameObject.layer)) != 0)
            return;

        if (!collision.gameObject.TryGetComponent<CustomTag>(out CustomTag t))
        {
            DestroyLemming();
            return;
        }

        foreach (string customTagEntry in t.Tags)
        {
            if (TagsLemmingsShouldReactTo.Contains(customTagEntry))
            {
                //Reflect!
                Reflect(collision);
            }
        }
    }

    public void InitLemming(LemmingsMarchManager _manager, float lemmingMoveSpeed, List<string> _TagsLemmingsHave, List<string> _TagsLemmingsShouldReactTo, LayerMask _layersLemmingsIgnore)
    {
        manager = _manager;
        moveSpeed = lemmingMoveSpeed;

        TagsLemmingsHave = _TagsLemmingsHave;
        TagsLemmingsShouldReactTo = _TagsLemmingsShouldReactTo;
        layersLemmingsIgnore = _layersLemmingsIgnore;

        lemmingCustomTags.Tags = TagsLemmingsHave;
        lemmingInitialized = true;
    }

    public void Reflect(Collision surfaceCollision)
    {
        //rb.excludeLayers = layersLemmingsIgnore;
        //coll.excludeLayers = layersLemmingsIgnore;

        //PlayRandomDeflectionSound(surfaceCollision.collider.GetComponent<AudioSource>());
        //InstantiateSpark(surfaceCollision.GetContact(0));

        // Prevent angular motion
        //rb.angularVelocity = Vector3.zero;
        //rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Get the contact point and normal
        ContactPoint contact = surfaceCollision.GetContact(0);
        Vector3 hitNormal = contact.normal;

        // Calculate the reflection direction
        Vector3 reflectedDirection = Vector3.Reflect(transform.forward.normalized, hitNormal);

        // Align the projectile's rotation with the reflected velocity
        Quaternion targetRotation = Quaternion.LookRotation(reflectedDirection);
        transform.rotation = targetRotation; //* Quaternion.Euler(90, 0, 0);

        // Calculate the new velocity while preserving speed
        //Vector3 velocityAfterReflection = reflectedDirection * incomingSpeed * reflectionForceMultiplier;

        // Debugging: Visualize the reflection
        Debug.DrawRay(contact.point, reflectedDirection, Color.blue, 3.0f);
    }

    void Wander()
    {
        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, wanderTarget) < 0.1f || Time.time > wanderTimer)
        {
            PickNewWanderTarget();
        }
    }

    void PickNewWanderTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = spawnCenter + new Vector3(randomOffset.x, 0, randomOffset.y);

        if (currentState == State.IdleWander)
        {
            transform.LookAt(new Vector3(wanderTarget.x, transform.position.y, wanderTarget.z));
            wanderTimer = Time.time + wanderTime;
        }

    }

    public void BeginMarching(Vector3 startingPosition)
    {
        currentState = State.Marching;
        gameObject.transform.position = startingPosition; 
        wanderTimer = -1;
    }

    public void BeginWandering(Vector3 startingPosition)
    {
        currentState = State.IdleWander;
        gameObject.transform.position = startingPosition;
    }

    public void DestroyLemming()
    {
        if (manager.destroyLemmingsIfRespawns)
        {
            if (OnDestroyEvent != null)
                OnDestroyEvent.Invoke(this);

            Destroy(this.gameObject);
        }
        else
        {
            if (OnRespawnEvent != null)
                OnRespawnEvent.Invoke(this);
        }
    }

    public void DestroyLemmingNoNotify()
    {
        if (manager.destroyLemmingsIfRespawns)
            Destroy(this.gameObject);
    }    

}