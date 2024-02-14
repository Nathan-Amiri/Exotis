using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestSwitch : MonoBehaviour
{
    // If a non-host client, the specified Transform will switch places with this one

    [SerializeField] private Transform transformToSwitchWith;

    private void OnEnable()
    {
        Setup.GuestSwitch += Switch;
    }

    private void OnDisable()
    {
        Setup.GuestSwitch -= Switch;
    }

    private void Switch()
    {
        (transformToSwitchWith.localPosition, transform.localPosition) = (transform.localPosition, transformToSwitchWith.localPosition);
    }
}