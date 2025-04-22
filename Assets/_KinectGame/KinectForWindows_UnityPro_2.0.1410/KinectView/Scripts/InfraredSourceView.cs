using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InfraredSourceView : MonoBehaviour 
{
    public GameObject InfraredSourceManager;
    private InfraredSourceManager _InfraredManager;
    
    void Start () 
    {
        if (gameObject.TryGetComponent<Renderer>(out Renderer rend))
            rend.material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }
    
    void Update()
    {
        if (InfraredSourceManager == null)
        {
            return;
        }
        
        _InfraredManager = InfraredSourceManager.GetComponent<InfraredSourceManager>();
        if (_InfraredManager == null)
        {
            return;
        }

        if (gameObject.TryGetComponent<Renderer>(out Renderer rend))
            rend.material.mainTexture = _InfraredManager.GetInfraredTexture();

        if (gameObject.TryGetComponent<Image>(out Image img))
            img.material.mainTexture = (Texture)_InfraredManager.GetInfraredTexture();
    }
}
