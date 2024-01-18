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

    public enum DelegationScenario { RoundStart, RoundEnd, TimeScale, Counter, Immediate, Reset}

    // Dynamic:
    private DelegationScenario delegationScenario;

    public delegate void NewActionNeeded(DelegationScenario scenario);
    public static event NewActionNeeded NewAction;

    public delegate void TurnAllUninteractableAction();
    public static event TurnAllUninteractableAction TurnAllUninteractable;

    // Called by ExecutionCore. ECore never passes in 'Reset'; Reset is only used by DCore to reset action buttons
    public void RequestDelegation(DelegationScenario newDelegationScenario)
    {
        delegationScenario = newDelegationScenario;

        NewAction?.Invoke(delegationScenario);

        // DelegationAction buttons turn interactable when appropriate. DelegationCore waits for an action to be selected
        // (ECore will not request a delegation if no non-pass action is available)
    }

    public void SelectAction(IDelegationAction action)
    {
        if (action.IsTargeted)
            Debug.Log("Is Targeted");
    }

    // Handle repopulation separately and manually in this class
}