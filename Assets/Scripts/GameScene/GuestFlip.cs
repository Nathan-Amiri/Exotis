using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestFlip : MonoBehaviour
{
    //flip y positions of certain scene objects for guest

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
        transform.position = new Vector2(transform.position.x, -transform.position.y);
    }
}