using Newtonsoft.Json.Converters;
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

    // SCENE REFERENCE:
    [SerializeField] private RelayCore relayCore;
    [SerializeField] private Console console;

    [SerializeField] private GameObject passButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private GameObject submitButton;

    [SerializeField] private List<GameObject> elementalTargetButtons = new();

    public enum DelegationScenario { RoundStart, RoundEnd, TimeScale, Counter, Reset}

    // DYNAMIC:
    private DelegationScenario delegationScenario;

    public delegate void NewActionNeeded(DelegationScenario scenario);
    public static event NewActionNeeded NewAction;

    // INPUT METHODS:
        // Called by ECore. ECore never passes in 'Reset'; Reset is only used by DCore to reset action buttons
    public void RequestDelegation(DelegationScenario newDelegationScenario, IDelegationAction immediateAction = null)
    {
        //.handle immediate manually without invoking NewAction

        delegationScenario = newDelegationScenario;

        if (newDelegationScenario == DelegationScenario.TimeScale)
            console.WriteConsoleMessage("Choose an action");
        else if (newDelegationScenario == DelegationScenario.Counter)
            console.WriteConsoleMessage("Choose a counter action");
        else if (newDelegationScenario != DelegationScenario.Reset)
        {
            string time = newDelegationScenario == DelegationScenario.RoundStart ? "beginning" : "end";
            console.WriteConsoleMessage("Choose an action to use at the " + time + " of the round");
        }

        passButton.SetActive(true);
        NewAction?.Invoke(delegationScenario);

        // DelegationAction buttons turn interactable when appropriate. DCore waits for an action to be selected
        // (ECore will not request a delegation if no non-pass action is available)
    }

        // Called by ECore
    public void RequestRepopulation()
    {

    }

    public void SelectCancel()
    {
        Reset();

        RequestDelegation(delegationScenario);
    }

    public void SelectPass()
    {
        passButton.SetActive(false);

        submitButton.SetActive(true);
        cancelButton.SetActive(true);
    }

    public void SelectAction(IDelegationAction action)
    {
        //.if Recharge or Hex, handle separately

        //int selfSlot = SlotAssignment.GetSlot(action.ParentElemental);
        //List<int> targetSlots = GetTargetSlots(selfSlot);

        //elementalTargetButtons[selfSlot].SetActive(action.CanTargetSelf);
        //elementalTargetButtons[targetSlots[0]].SetActive(action.CanTargetAlly);
        //elementalTargetButtons[targetSlots[1]].SetActive(action.CanTargetEnemy);
        //elementalTargetButtons[targetSlots[2]].SetActive(action.CanTargetEnemy);
        //elementalTargetButtons[targetSlots[3]].SetActive(action.CanTargetBenchedAlly);
        //elementalTargetButtons[targetSlots[4]].SetActive(action.CanTargetBenchedAlly);
    }

    public void SelectTarget(Elemental target)
    {

    }

    public void SelectSubmit()
    {

    }

    public void SelectHexEffect(int hexEffect)
    {
        // 0 = Slow, 1 = Poison, 2 = Weaken

    }

    // HELPER METHODS:
    private bool CheckTargetAvailable(int slot)
    {
        //.check whether that slot contains an Elemental, and if they're Disengaged

        return true;
    }

    // STATE METHODS:

    private void RepopulationRequested()
    {

    }

    private void ChooseTarget()
    {
        //.no target available
        //.display submit if a target's already selected
        //.potion interactable controlled here
        //.message disappears if all available targets are chosen
    }

    private void Reset()
    {
        cancelButton.SetActive(false);
        submitButton.SetActive(false);
        NewAction?.Invoke(DelegationScenario.Reset);
    }

    private void RechargeDelegation()
    {

    }

    private void HexDelegation()
    {

    }
}