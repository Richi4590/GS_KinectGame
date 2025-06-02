using BezierSolution;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEventStopPlayer : OnTriggerEvent
{
    public bool stopIfTrigger = false;

    public override void OnCollisionEnter(Collision collision)
    {
        if (!stopIfTrigger && CheckIfCollisionObjectHasCustomTag(collision))
        {
            _OnCollisionEnter.Invoke();
            BezierWalkerWithSpeed.Instance.StopMoving();
        }
    }

    public override void OnCollisionStay(Collision collision)
    {
        if (CheckIfCollisionObjectHasCustomTag(collision))
            _OnCollisionStay.Invoke();
    }

    public override void OnCollisionExit(Collision collision)
    {
        if (CheckIfCollisionObjectHasCustomTag(collision))
            _OnCollisionEnter.Invoke();

    }

    public override void OnTriggerEnter(Collider other)
    {
        if (stopIfTrigger && CheckIfColliderHasCustomTag(other))
        {
            _OnTriggerEnter.Invoke();
            BezierWalkerWithSpeed.Instance.StopMoving();
        }
    }

    public override void OnTriggerStay(Collider other)
    {
        if (CheckIfColliderHasCustomTag(other))
            _OnTriggerStay.Invoke();
    }

    public override void OnTriggerExit(Collider other)
    {
        if (CheckIfColliderHasCustomTag(other))
            _OnTriggerLeave.Invoke();
    }
}
