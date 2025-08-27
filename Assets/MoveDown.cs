using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveDown : MonoBehaviour
{
    public bool shouldMoveDown = false;   // Toggle movement
    public float moveDownAmount = 1f;     // How far down to move
    public float speed = 2f;              // How fast to move

    private Vector3 initialPosition;      // Starting position
    private Vector3 targetPosition;       // Where to move down to

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition - new Vector3(0, moveDownAmount, 0);
    }

    void Update()
    {
        if (shouldMoveDown)
        {
            // Smoothly move toward the lower position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
        else
        {
            // Smoothly move back up to the initial position
            transform.position = Vector3.MoveTowards(transform.position, initialPosition, speed * Time.deltaTime);
        }
    }

    public void ShouldMoveDown(bool newShouldMoveDownState)
    {
        shouldMoveDown = newShouldMoveDownState;
    }
}