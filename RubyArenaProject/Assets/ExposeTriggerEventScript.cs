using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExposeTriggerEventScript : MonoBehaviour
{
    public Action<Collider> onTriggerEnter ;
    public Action<Collider> onTriggerExit ;

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke(other);
    }
}
