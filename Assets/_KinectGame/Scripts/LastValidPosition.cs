using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastValidPosition : MonoBehaviour
{
    public LayerMask groundLayer; // Assign in Inspector

    [SerializeField] private Vector2 lastValidPosition;

    void Update()
    {
        if ((transform.position.x != lastValidPosition.x || transform.position.y != lastValidPosition.y) && IsGrounded())
        {
            lastValidPosition = transform.position;
        }
    }

    public Vector2 GetLastValidPosition()
    {
        return lastValidPosition;
    }

    bool IsGrounded()
    {
        // Cast a ray downward from the player's feet
        return Physics2D.Raycast(transform.position, Vector2.down, 1, groundLayer);
    }
}
