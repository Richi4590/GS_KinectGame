using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraSettingsSetter : GameConfigLoaderForClass
{
    [SerializeField] private Camera cam;
    public override void LoadGameConfigForClass()
    { 
        Vector3 camPos = cam.gameObject.transform.position;
        camPos.z = float.Parse(LoadGameConfig.gameConfigMap["kinectCameraHeight"]);
        cam.gameObject.transform.position = camPos;

        cam.orthographicSize = float.Parse(LoadGameConfig.gameConfigMap["kinectCameraScale"]); 
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
