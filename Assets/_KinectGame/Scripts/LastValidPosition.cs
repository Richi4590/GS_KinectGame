using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastValidPosition : MonoBehaviour
{
    public LayerMask groundLayer;
    public float interval = 1f;
    public float rayLength = 1.5f;
    public int maxValidPositions = 4;
    public Vector3 rayOffset = Vector3.zero;

    private float timer = 0f;
    private Queue<Vector2> lastValidPositions = new Queue<Vector2>();

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;


            Vector3 Tposition = transform.position + rayOffset;
            Vector2 TcurrentPosition = new Vector2(Tposition.x, Tposition.z);

            if (IsGrounded())
            {
                Vector3 position = transform.position + rayOffset;
                Vector2 currentPosition = new Vector2(position.x, position.z);
                AddValidPosition(currentPosition);
            }
        }
    }

    void AddValidPosition(Vector2 pos)
    {
        if (lastValidPositions.Count >= maxValidPositions)
        {
            lastValidPositions.Dequeue(); // Remove oldest
        }

        lastValidPositions.Enqueue(pos); // Add newest
    }

    public Vector2 GetMostRecentValidPosition()
    {
        if (lastValidPositions.Count == 0)
            return transform.position; // fallback

        return lastValidPositions.Peek(); // last added is most recent
    }

    bool IsGrounded()
    {
        Vector3 position = transform.position + rayOffset;
        Vector2 direction = Vector2.down;


        #if UNITY_EDITOR
        Debug.DrawRay(position, direction * rayLength, Color.yellow, 0.1f);
        #endif

        // Perform the raycast
        return Physics.Raycast(position, direction, rayLength, groundLayer);
    }
}
