using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class CatDeathPhaseOutShaderMulti : MonoBehaviour
{
    [Min(0)] public float animationDuration = 1f;

    [SerializeField] private bool getListOfMeshRenderersAutomatically = false;
    [SerializeField] private List<MeshRenderer> rendererList;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (getListOfMeshRenderersAutomatically)
            rendererList = gameObject.GetComponentsInChildren<MeshRenderer>().ToList();
    }

    public void StartPhaseOut()
    {
        StartCoroutine(PhaseOut());
    }

    private IEnumerator PhaseOut()
    {
        float time = 0f;

        while (time < animationDuration)
        {
            float t = time / animationDuration;

            foreach (MeshRenderer rend in rendererList)
            {
                // Update property block instead of material
                rend.GetPropertyBlock(block);
                block.SetFloat("_PhasingOut", t);
                rend.SetPropertyBlock(block);
            }

            time += Time.deltaTime;
            yield return null;
        }


        foreach (MeshRenderer rend in rendererList)
        {
            // Ensure it finishes at 1
            rend.GetPropertyBlock(block);
            block.SetFloat("_PhasingOut", 1f);
            rend.SetPropertyBlock(block);
        }

        Destroy(this.transform.root.gameObject);
    }
}