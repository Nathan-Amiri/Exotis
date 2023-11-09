using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelegationCore : MonoBehaviour
{
    //this class handles the logic pertaining to the player
    //choosing an action. Once an action has been submitted,
    //DelegationCore sends a RelayPacket to RelayCore providing
    //the details of the action.

    //Core logic classes can only impact each other in one direction:
    //DelegationCore > RelayCore > ExecutionCore > DelegationCore

    //assigned in inspector:
    [SerializeField] private RelayCore relayCore;

    public enum DelegationType { RoundStartOrEnd, TimeScale, Counter, Repopulate, Immediate} //combine roundstartorend and timescale? Don't redo what clock already does!

    //dynamic:
    private DelegationType delegationType;

    public void RequestDelegation(DelegationType newDelegationType, bool passAvailable = true) //offer player an action
    {

    }
}