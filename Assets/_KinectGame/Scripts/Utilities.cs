using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utilities : MonoBehaviour
{
    public static bool IsInSelectedLayers(LayerMask layersToCheck, GameObject obj)
    {
        // Get the layer of the current object
        int objectLayer = obj.layer;

        // Check if the object's layer is in the selected layers mask
        return (layersToCheck.value & (1 << objectLayer)) != 0;
    }
}
