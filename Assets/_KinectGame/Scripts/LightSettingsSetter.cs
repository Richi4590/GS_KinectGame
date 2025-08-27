using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSettingsSetter : MonoBehaviour
{
    private Light l;
    [SerializeField] private bool isShape = false;

    // Start is called before the first frame update
    void Start()
    {
        l = GetComponent<Light>();

        if (isShape)
        {
            l.range = l.range * LightConfig.Instance.shapeRangeMultiplier;
            l.intensity = l.intensity * LightConfig.Instance.shapeIntensityMultiplier;
        }
        else
        {
            l.range = l.range * LightConfig.Instance.rangeMultiplier;
            l.intensity = l.intensity * LightConfig.Instance.intensityMultiplier;
        }

    }
}
