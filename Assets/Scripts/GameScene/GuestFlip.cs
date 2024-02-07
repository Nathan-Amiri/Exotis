using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestFlip : MonoBehaviour
{
    // If a non-host client, all scene objects with this script attached have their y positions flipped

    private void OnEnable()
    {
        Setup.GuestFlip += Flip;
    }

    private void OnDisable()
    {
        Setup.GuestFlip -= Flip;
    }

    private void Flip()
    {
        transform.localPosition = new Vector2(transform.localPosition.x, -transform.localPosition.y);
    }
}