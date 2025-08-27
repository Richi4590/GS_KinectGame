using System;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    [Header("Plate Settings")]
    public bool isActivated = false; // Check if plate is pressed
    public Transform plateVisual;
    public Light activationLight;
    public float pressDepth = 0.1f;  // How much the plate sinks when pressed
    public float pressSpeed = 5f;    // Smooth movement speed
    public float releaseDelay = 2f;  // Time to stay pressed after release
    public List<string> TagsToReactTo = new List<string>();
    public UnityEvent actionWhenPressed;
    public UnityEvent actionWhenUnpressed;


    private Vector3 initialPos;
    private int objectsOnPlate = 0;
    private MaterialPropertyBlock block;
    private MeshRenderer meshRenderer;
    private float releaseTimer = 0f;

    void Start()
    {
        if (plateVisual != null)
            initialPos = plateVisual.localPosition;

        block = new MaterialPropertyBlock();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && Utilities.HasCustomTag(other.gameObject, TagsToReactTo))
        {
            objectsOnPlate++;
            isActivated = true;
            activationLight.enabled = true;
            releaseTimer = 0f; // reset timer when pressed again

            block.SetInt("_Pressed", Convert.ToInt32(isActivated));
            meshRenderer.SetPropertyBlock(block);
            actionWhenPressed.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && Utilities.HasCustomTag(other.gameObject, TagsToReactTo))
        {
            objectsOnPlate--;

            if (objectsOnPlate <= 0)
            {
                objectsOnPlate = 0;
                releaseTimer = releaseDelay; // start countdown
            }
        }
    }

    void Update()
    {
        // Countdown when no one is on the plate
        if (objectsOnPlate == 0 && releaseTimer > 0f)
        {
            releaseTimer -= Time.deltaTime;
            if (releaseTimer <= 0f)
            {
                isActivated = false;
                activationLight.enabled = false;

                block.SetInt("_Pressed", Convert.ToInt32(isActivated));
                meshRenderer.SetPropertyBlock(block);

                actionWhenUnpressed.Invoke();
            }
        }

        // Animate plate position
        if (plateVisual != null)
        {
            Vector3 targetPos = initialPos;

            if (isActivated)
                targetPos = initialPos - new Vector3(0, pressDepth, 0);

            plateVisual.localPosition = Vector3.Lerp(
                plateVisual.localPosition,
                targetPos,
                Time.deltaTime * pressSpeed
            );
        }
    }
}