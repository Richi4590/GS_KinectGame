using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightConfig : GameConfigLoaderForClass
{
    public static LightConfig Instance;

    public float rangeMultiplier = 1.0f;
    public float intensityMultiplier = 1.0f;


    public float shapeRangeMultiplier = 1.0f;
    public float shapeIntensityMultiplier = 1.0f;

    public override void LoadGameConfigForClass()
    {
        rangeMultiplier = (float)double.Parse(LoadGameConfig.gameConfigMap["lightRangeMultiplier"]);
        intensityMultiplier = (float)double.Parse(LoadGameConfig.gameConfigMap["lightIntensityMultiplier"]);

        shapeRangeMultiplier = (float)double.Parse(LoadGameConfig.gameConfigMap["lightShapeRangeMultiplier"]);
        shapeIntensityMultiplier = (float)double.Parse(LoadGameConfig.gameConfigMap["lightShapeIntensityMultiplier"]);
    }

    private void Awake()
    {
        if (LightConfig.Instance != null)
            Destroy(this);
        else
            LightConfig.Instance = this;
    }
}
