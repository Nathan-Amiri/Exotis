using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DelegationCore : MonoBehaviour
{
    // This class handles the logic pertaining to the player
    // choosing an action. Once an action has been submitted,
    // DelegationCore sends a RelayPacket to RelayCore providing
    // the details of the action.

    // Core logic classes can only impact each other in one direction:
    // DelegationCore > RelayCore > ExecutionCore > DelegationCore

    // STATIC:
    public delegate void NewActionNeeded(DelegationScenario scenario);
    public static event NewActionNeeded NewAction;

    // SCENE REFERENCE:
    [SerializeField] private RelayCore relayCore;
    [SerializeField] private Console console;
    [SerializeField] private GameObject consoleButton;

    [SerializeField] private GameObject passButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private GameObject submitButton;

    [SerializeField] private List<GameObject> elementalTargetButtons = new();

    [SerializeField] private List<Button> potionButtons = new();

    public enum DelegationScenario { RoundStart, RoundEnd, TimeScale, Counter, Immediate, Repopulation, Reset}

    // DYNAMIC:
    private DelegationScenario delegationScenario;

    private RelayPacket packet;
    private IDelegationAction currentAction;

    // Called by ECore. ECore never passes in 'Reset'; Reset is only used by DCore to reset action buttons
    public void RequestDelegation(DelegationScenario newDelegationScenario, IDelegationAction immediateAction = null)
    {
        delegationScenario = newDelegationScenario;

        if (newDelegationScenario == DelegationScenario.Immediate)
        {
            if (immediateAction == null)
            {
                Debug.LogError("Immediate action is null");
                return;
            }

            console.WriteConsoleMessage("Activate " + immediateAction.ParentElemental.name + "'s " + immediateAction.Name + "?");

            passButton.SetActive(true);
            immediateAction.OnNewActionNeeded(DelegationScenario.Immediate);

            return;
        }

        // Repopulation only requires a delegation if there are two allies on the Bench
        if (newDelegationScenario == DelegationScenario.Repopulation)
        {
            console.WriteConsoleMessage("Choose a Benched Elemental to Swap into play");

            if (NetworkManager.Singleton.IsHost)
            {
                elementalTargetButtons[4].SetActive(true);
                elementalTargetButtons[5].SetActive(true);
            }
            else
            {
                elementalTargetButtons[6].SetActive(true);
                elementalTargetButtons[7].SetActive(true);
            }

            return;
        }

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

    public void SelectCancel()
    {
        ResetScene();
        packet = default;

        // If Immediate, immediate action must be passed into RequestDelegation
        if (delegationScenario == DelegationScenario.Immediate)
            RequestDelegation(delegationScenario, currentAction);
        else
        {
            currentAction = null;
            RequestDelegation(delegationScenario);
        }
    }

    public void SelectPass()
    {
        ResetScene();

        submitButton.SetActive(true);
        cancelButton.SetActive(true);

        packet.actionType = "pass";
    }

    public void SelectAction(IDelegationAction action)
    {
        //.if Recharge or Hex, handle separately

        currentAction = action;

        // Set packet info
        packet.actionType = currentAction.ActionType;
        packet.casterSlot = SlotAssignment.GetSlot(currentAction.ParentElemental);
        if (currentAction.ActionType == "spell")
            packet.name = action.Name;

        // Reset before proceeding
        ResetScene();

        // Check if the action is untargeted
        if (currentAction.MaxTargets == 0)
        {
            console.WriteConsoleMessage(string.Empty);
            cancelButton.SetActive(true);
            submitButton.SetActive(true);

            return;
        }

        // Check if the action has an available target
        if (!CheckTargetAvailable(packet.casterSlot))
        {
            packet = default;

            console.WriteConsoleMessage("No available targets. Choose a different action");
            consoleButton.SetActive(true);

            return;
        }

        // Turn on target buttons
        string message = action.MaxTargets == 1 ? "Choose a target" : "Choose target(s)";
        console.WriteConsoleMessage(message);
        cancelButton.SetActive(true);

        Dictionary<string, int> potentialTargetSlots = SlotAssignment.GetSlotDesignations(packet.casterSlot);

        elementalTargetButtons[packet.casterSlot].SetActive(currentAction.CanTargetSelf);
        elementalTargetButtons[potentialTargetSlots["allySlot"]].SetActive(currentAction.CanTargetAlly);
        elementalTargetButtons[potentialTargetSlots["enemy1Slot"]].SetActive(currentAction.CanTargetEnemy);
        elementalTargetButtons[potentialTargetSlots["enemy2Slot"]].SetActive(currentAction.CanTargetEnemy);
        elementalTargetButtons[potentialTargetSlots["benchedAlly1Slot"]].SetActive(currentAction.CanTargetBenchedAlly);
        elementalTargetButtons[potentialTargetSlots["benchedAlly2Slot"]].SetActive(currentAction.CanTargetBenchedAlly);
    }

    public void SelectConsoleButton()
    {
        consoleButton.SetActive(false);

        RequestDelegation(delegationScenario);
    }

    public void SelectTarget(int targetSlot)
    {
        // Add value to array
        if (packet.targetSlots == null)
            packet.targetSlots = new int[] { targetSlot };
        else
        {
            List<int> temp = packet.targetSlots.ToList();
            temp.Add(targetSlot);
            packet.targetSlots = temp.ToArray();
        }

        // Make Potion interactable if currentAction is a single-target damaging Spell
        potionButtons[packet.casterSlot].interactable = PotionInteractable();

        // Turn off unavailable target buttons
        elementalTargetButtons[targetSlot].SetActive(false);

            // Check for Repopulation first since currentAction is null when Repopulating
        if (delegationScenario == DelegationScenario.Repopulation || packet.targetSlots.Length == currentAction.MaxTargets)
            foreach (GameObject button in elementalTargetButtons)
                button.SetActive(false);

        // Remove console message if no more targets are available
        bool moreTargetsAvailable = false;
        foreach (GameObject button in elementalTargetButtons)
            if (button.activeSelf)
                moreTargetsAvailable = true;
        if (!moreTargetsAvailable)
            console.WriteConsoleMessage(string.Empty);

        // Allow packet to be submitted
        submitButton.SetActive(true);
        cancelButton.SetActive(true);
    }
    private bool PotionInteractable()
    {
        // CurrentAction is null when repopulating
        if (currentAction == null)
            return false;

        if (!currentAction.ParentElemental.hasPotion)
            return false;

        if (currentAction is not Spell currentSpell)
            return false;

        if (!currentSpell.IsDamaging)
            return false;

        if (packet.targetSlots.Length != 1)
            return false;

        return true;
    }

    public void SelectPotion()
    {
        packet.potion = true;

        // Turn off any remaining target buttons
        foreach (GameObject button in elementalTargetButtons)
            button.SetActive(false);
        console.WriteConsoleMessage(string.Empty);
    }

    public void SelectSubmit()
    {
        ResetScene();

        console.WriteConsoleMessage("Waiting for enemy");

        packet.player = NetworkManager.Singleton.IsHost ? 0 : 1;
        relayCore.PrepareToRelayPacket(packet);

        packet = default;
        currentAction = null;
    }

    private bool CheckTargetAvailable(int slot)
    {
        Elemental target = SlotAssignment.Elementals[slot];

        // Slot does not contain an Elemental
        if (target == null)
            return false;

        // Slot contains an Elemental that is Disengaged
        if (target.isDisengaged)
            return false;

        return true;
    }

    private void ResetScene()
    {
        passButton.SetActive(false);
        cancelButton.SetActive(false);
        submitButton.SetActive(false);

        foreach (GameObject button in elementalTargetButtons)
            button.SetActive(false);

        NewAction?.Invoke(DelegationScenario.Reset);
    }
}