using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SendMesseger : MonoBehaviour
{
    [Header("Send Message Settings")]
    public List<string> TagsToReactTo = new List<string>();
    public List<string> functionStrings = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enabled)
        {
            if (Utilities.HasCustomTag(collision.gameObject, TagsToReactTo))
                foreach (string functionString in functionStrings)
                    collision.gameObject.SendMessage(functionString, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enabled)
        {
            if (Utilities.HasCustomTag(other.gameObject, TagsToReactTo))
                foreach (string functionString in functionStrings)
                    other.gameObject.SendMessage(functionString, SendMessageOptions.DontRequireReceiver);
        }
    }
}
