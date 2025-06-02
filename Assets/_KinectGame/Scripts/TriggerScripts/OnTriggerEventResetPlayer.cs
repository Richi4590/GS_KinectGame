using BezierSolution;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEventResetPlayer : OnTriggerEvent
{
    public override void OnCollisionEnter(Collision collision)
    {
        if (TagsToReactUpon.Contains(collision.gameObject.tag))
        {
            _OnCollisionEnter.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }
    }

    public override void OnCollisionStay(Collision collision)
    {
        if (TagsToReactUpon.Contains(collision.gameObject.tag))
        {
            _OnCollisionStay.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }
    }

    public override void OnCollisionExit(Collision collision)
    {
        if (TagsToReactUpon.Contains(collision.gameObject.tag))
        {
            _OnCollisionLeave.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }

    }

    public override void OnTriggerEnter(Collider other)
    {
        if (TagsToReactUpon.Contains(other.gameObject.tag))
        {
            _OnTriggerEnter.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }
    }

    public override void OnTriggerStay(Collider other)
    {
        if (TagsToReactUpon.Contains(other.gameObject.tag))
        {
            _OnTriggerStay.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }
    }

    public override void OnTriggerExit(Collider other)
    {
        if (TagsToReactUpon.Contains(other.gameObject.tag))
        {
            _OnTriggerLeave.Invoke();
            BezierWalkerWithSpeed.Instance.ResetToLastCheckpoint();
        }
    }



}
