using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTag : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetNewTag(string newTag)
    {
        gameObject.tag = newTag;
    }
}
