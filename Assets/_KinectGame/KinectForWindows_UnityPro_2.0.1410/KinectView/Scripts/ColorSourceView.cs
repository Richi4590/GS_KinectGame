using UnityEngine;
using System.Collections;
using Windows.Kinect;
using UnityEngine.UI;

public class ColorSourceView : MonoBehaviour
{
    public GameObject ColorSourceManager;
    private ColorSourceManager _ColorManager;
    
    void Start ()
    {
        if (gameObject.TryGetComponent<Renderer>(out Renderer rend))
            rend.material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }
    
    void Update()
    {
        if (ColorSourceManager == null)
        {
            return;
        }
        
        _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();
        if (_ColorManager == null)
        {
            return;
        }
        
        if (gameObject.TryGetComponent<Renderer>(out Renderer rend))
            rend.material.mainTexture = _ColorManager.GetColorTexture();

        if (gameObject.TryGetComponent<Image>(out Image img))
            img.material.mainTexture = (Texture)_ColorManager.GetColorTexture();
    }
}
