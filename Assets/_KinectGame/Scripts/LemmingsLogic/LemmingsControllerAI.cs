using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LemmingsControllerAI : MonoBehaviour
{
    public enum State { IdleWander, Marching }
    public State currentState = State.IdleWander;

    public float wanderRadius = 5f;
    public float moveSpeed = 2f;
    public event UnityAction<LemmingsControllerAI> OnDestroyEvent;

    private Vector3 spawnCenter;
    private Vector3 wanderTarget;

    private float wanderTime = 2f;
    private float wanderTimer;

    void Start()
    {
        spawnCenter = transform.position;
        PickNewWanderTarget();
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
        transform.LookAt(new Vector3(wanderTarget.x, transform.position.y, wanderTarget.z));
        wanderTimer = Time.time + wanderTime;
    }

    public void BeginMarching()
    {
        currentState = State.Marching;
    }

    public void BeginMarching(Vector3 startingPosition)
    {
        gameObject.transform.position = startingPosition; 
        currentState = State.Marching;
    }

    public void DestroyLemming()
    {
        if (OnDestroyEvent != null)
            OnDestroyEvent.Invoke(this);

        Destroy(this.gameObject);
    }

    public void DestroyLemmingNoNotify()
    {
        Destroy(this.gameObject);
    }    

}