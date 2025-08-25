using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class CatDeathPhaseOutShader : MonoBehaviour
{
    [Min(0)] public float animationDuration = 1f;

    private Renderer rend;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
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

            // Update property block instead of material
            rend.GetPropertyBlock(block);
            block.SetFloat("_PhasingOut", t);
            rend.SetPropertyBlock(block);

            time += Time.deltaTime;
            yield return null;
        }

        // Ensure it finishes at 1
        rend.GetPropertyBlock(block);
        block.SetFloat("_PhasingOut", 1f);
        rend.SetPropertyBlock(block);

        Destroy(this.transform.root.gameObject);
    }
}