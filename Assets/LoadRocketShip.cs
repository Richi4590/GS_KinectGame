using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadRocketShip : MonoBehaviour
{
    public int requiredLemmings = 10;
    private int lemmingsOnBoard = 0;

    public List<MeshRenderer> renderers;
    private MaterialPropertyBlock block;


    // Start is called before the first frame update
    void Start()
    {
        block = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetShaderLoadLevelRocketShip(int amountOnBoard)
    {
        foreach (MeshRenderer rocketShipPartRenderer in renderers)
        {
            // Update property block instead of material
            rocketShipPartRenderer.GetPropertyBlock(block);
            block.SetFloat("_Current", amountOnBoard);
            rocketShipPartRenderer.SetPropertyBlock(block);
        }
    }

    public void LemmingArrived()
    {
        lemmingsOnBoard++;
        SetShaderLoadLevelRocketShip(lemmingsOnBoard);
    }
}
