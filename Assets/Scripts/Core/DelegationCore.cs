using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelegationCore : MonoBehaviour
{
    // This class handles the logic pertaining to the player
    // choosing an action. Once an action has been submitted,
    // DelegationCore sends a RelayPacket to RelayCore providing
    // the details of the action.

    // Core logic classes can only impact each other in one direction:
    // DelegationCore > RelayCore > ExecutionCore > DelegationCore

    // Assigned in inspector:
    [SerializeField] private RelayCore relayCore;

    public enum DelegationScenario { RoundStart, RoundEnd, TimeScale, Counter, Immediate}

    // Dynamic:
    private DelegationScenario delegationScenario;

    public delegate void NewDelegationAction(DelegationScenario scenario);
    public static event NewDelegationAction NewDelegation;

    public void RequestDelegation(DelegationScenario newDelegationScenario)
    {
        delegationScenario = newDelegationScenario;

        NewDelegation?.Invoke(delegationScenario);
    }

    public void SelectAction(IDeclarable declaredAction)
    {
        if (declaredAction.IsTargeted)
            Debug.Log("Is Targeted");

        // Possible next steps: cancel, submit, target, fail, and misc (rechex, potion/frenzy)
    }

    // Handle repopulation separately and manually in this class
}