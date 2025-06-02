using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEvent : MonoBehaviour
{
    public List<string> TagsToReactUpon = new List<string>();

    public UnityEvent _OnCollisionEnter = new UnityEvent();
    public UnityEvent _OnCollisionStay = new UnityEvent();
    public UnityEvent _OnCollisionLeave = new UnityEvent();

    public UnityEvent _OnTriggerEnter = new UnityEvent();
    public UnityEvent _OnTriggerStay = new UnityEvent();
    public UnityEvent _OnTriggerLeave = new UnityEvent();

    public virtual void OnCollisionEnter(Collision collision)
    {
        if (CheckIfCollisionObjectHasCustomTag(collision))
            _OnCollisionEnter.Invoke();
    }

    public virtual void OnCollisionStay(Collision collision)
    {
        if (CheckIfCollisionObjectHasCustomTag(collision))
            _OnCollisionStay.Invoke();
    }

    public virtual void OnCollisionExit(Collision collision)
    {
        if (CheckIfCollisionObjectHasCustomTag(collision))
            _OnCollisionEnter.Invoke();

    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (CheckIfColliderHasCustomTag(other))
            _OnTriggerEnter.Invoke();
    }

    public virtual void OnTriggerStay(Collider other)
    {
        if (CheckIfColliderHasCustomTag(other))
            _OnTriggerStay.Invoke();
    }

    public virtual void OnTriggerExit(Collider other)
    {
        if (CheckIfColliderHasCustomTag(other))
            _OnTriggerLeave.Invoke();
    }

    protected bool CheckIfCollisionObjectHasCustomTag(Collision c)
    {
        if (c.gameObject.TryGetComponent<CustomTag>(out CustomTag t))
        {
            foreach (string customTagEntry in t.Tags)
            {
                if (TagsToReactUpon.Contains(customTagEntry))
                {
                    return true;
                }
            }
            return false;
        }

        return false;
    }

    protected bool CheckIfColliderHasCustomTag(Collider c)
    {
        if (c.gameObject.TryGetComponent<CustomTag>(out CustomTag t))
        {
            foreach (string customTagEntry in t.Tags)
            {
                if (TagsToReactUpon.Contains(customTagEntry))
                {
                    return true;
                }
            }
            return false;
        }

        return false;
    }

}
