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
    public delegate void NewActionNeeded(bool reset = false);
    public static event NewActionNeeded NewAction;

    // SCENE REFERENCE:
    [SerializeField] private RelayCore relayCore;
    [SerializeField] private Console console;
    [SerializeField] private GameObject consoleButton;

    [SerializeField] private GameObject passButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private GameObject submitButton;

    [SerializeField] private List<GameObject> wildButtons = new();
    [SerializeField] private List<GameObject> rechargeButtons = new();
    [SerializeField] private List<GameObject> hexButtons = new();

    [SerializeField] private List<GameObject> elementalTargetButtons = new();

    [SerializeField] private List<Button> potionButtons = new();

    // DYNAMIC:
    private RelayPacket packet;
    private IDelegationAction currentAction;

    // Called by ECore. ECore never passes in 'Reset'; Reset is only used by DCore to reset action buttons
    public void RequestDelegation(IDelegationAction immediateAction = null)
    {
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
        {
            if (immediateAction == null)
            {
                Debug.LogError("Immediate action is null");
                return;
            }

            console.WriteSingleConsoleMessage("Activate " + immediateAction.ParentElemental.name + "'s " + immediateAction.Name + "?", true);

            passButton.SetActive(true);
            immediateAction.OnNewActionNeeded();

            return;
        }

        // Repopulation only requires a delegation if there are two allies on the Bench
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
        {
            console.WriteSingleConsoleMessage("Choose a Benched Elemental to Swap into play", true);

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

        if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
            console.WriteSingleConsoleMessage("Choose an action", true);
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            console.WriteSingleConsoleMessage("Choose a counter action", true);
        else
        {
            string time = Clock.CurrentRoundState == Clock.RoundState.RoundStart ? "beginning" : "end";
            console.WriteSingleConsoleMessage("Choose an action to use at the " + time + " of the round", true);
        }

        passButton.SetActive(true);
        NewAction?.Invoke();

        // DelegationAction buttons turn interactable when appropriate. DCore waits for an action to be selected
        // (ECore will not request a delegation if no non-pass action is available)
    }

    public void SelectCancel()
    {
        ResetScene();
        packet = default;

        // If Immediate, immediate action must be passed into RequestDelegation
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
            RequestDelegation(currentAction);
        else
        {
            currentAction = null;
            RequestDelegation();
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
        currentAction = action;

        if (currentAction is Spell spell)
        {
            packet.name = spell.name;

            if (spell.IsWild && packet.wildTimeScale == 0)
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteSingleConsoleMessage("At what time?", true);

                wildButtons[0].SetActive(true);
                wildButtons[1].SetActive(Clock.CurrentTimeScale >= 5);

                return;
            }
            else if (spell.name == "Hex" && packet.hexType == null)
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteSingleConsoleMessage("Choose an effect", true);
                foreach (GameObject button in hexButtons)
                    button.SetActive(true);

                return;
            }
        }
        else
            spell = null;

        // Set packet info
        packet.actionType = currentAction.ActionType;
        packet.casterSlot = SlotAssignment.GetSlot(currentAction.ParentElemental);

        // Reset before proceeding
        ResetScene();

        // Check if the action is untargeted
        if (currentAction.MaxTargets == 0)
        {
            console.HideConsole();
            cancelButton.SetActive(true);
            submitButton.SetActive(true);

            return;
        }

        // Get targetable slots
        List<int> availableTargetSlots = new();

        Dictionary<string, int> potentialTargetSlots = SlotAssignment.GetSlotDesignations(packet.casterSlot);

        if (currentAction.CanTargetSelf)
            availableTargetSlots.Add(packet.casterSlot);

        int allySlot = potentialTargetSlots["allySlot"];
        if (currentAction.CanTargetAlly && CheckTargetAvailable(allySlot))
            availableTargetSlots.Add(allySlot);

        int enemy1Slot = potentialTargetSlots["enemy1Slot"];
        if (currentAction.CanTargetEnemy)
        {
            if (CheckTargetAvailable(enemy1Slot))
                availableTargetSlots.Add(enemy1Slot);
            // Enemy 2
            if (CheckTargetAvailable(enemy1Slot + 1))
                availableTargetSlots.Add(enemy1Slot + 1);
        }

        int benchedAlly1Slot = potentialTargetSlots["benchedAlly1Slot"];
        if (currentAction.CanTargetBenchedAlly)
        {
            if (CheckTargetAvailable(benchedAlly1Slot))
                availableTargetSlots.Add(benchedAlly1Slot);
            // Benched ally 2
            if (CheckTargetAvailable(benchedAlly1Slot + 1))
                availableTargetSlots.Add(benchedAlly1Slot + 1);
        }

        // Check if there's no available targets
        if (availableTargetSlots.Count == 0)
        {
            packet = default;

            console.WriteSingleConsoleMessage("No available targets. Choose a different action", true);
            consoleButton.SetActive(true);

            return;
        }

        // Turn on target buttons
        foreach (int availableTargetSlot in availableTargetSlots)
            elementalTargetButtons[availableTargetSlot].SetActive(true);

        string message = action.MaxTargets == 1 ? "Choose a target" : "Choose target(s)";

        if (spell != null && spell.name == "Recharge")
            message = "Choose an ally to heal 1 and gain a Spark";

        console.WriteSingleConsoleMessage(message, true);
        cancelButton.SetActive(true);
    }

    public void SelectWildButton(int wildTimeScale)
    {
        packet.wildTimeScale = wildTimeScale;

        SelectAction(currentAction);
    }
    public void SelectHexButton(string hexType)
    {
        packet.hexType = hexType;

        SelectAction(currentAction);
    }

    public void SelectConsoleButton()
    {
        consoleButton.SetActive(false);

        RequestDelegation();
    }

    public void SelectTarget(int targetSlot)
    {
        // Add value to packet's targetSlots array
        if (packet.targetSlots == null)
            packet.targetSlots = new int[] { targetSlot };
        else
        {
            List<int> temp = packet.targetSlots.ToList();
            temp.Add(targetSlot);
            packet.targetSlots = temp.ToArray();
        }

        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
        {
            foreach (GameObject button in elementalTargetButtons)
                button.SetActive(false);

            console.HideConsole();

            submitButton.SetActive(true);
            cancelButton.SetActive(true);
        }

        if (currentAction is Spell spell && spell.name == "Recharge" && packet.rechargeType == null)
        {
            ResetScene();

            int targetAllySlot = SlotAssignment.GetSlotDesignations(targetSlot)["allySlot"];
            if (!CheckTargetAvailable(targetAllySlot))
            {
                cancelButton.SetActive(true);
                submitButton.SetActive(true);

                return;
            }

            console.WriteSingleConsoleMessage("Will " + SlotAssignment.Elementals[targetAllySlot].name + " heal 1 or gain a Spark?", true);

            cancelButton.SetActive(true);
            foreach (GameObject button in rechargeButtons)
                button.SetActive(true);

            return;
        }

        // Make Potion interactable if currentAction is a single-target damaging Spell
        potionButtons[packet.casterSlot].interactable = PotionInteractable();

        // Turn off unavailable target buttons
        elementalTargetButtons[targetSlot].SetActive(false);

        if (packet.targetSlots.Length == currentAction.MaxTargets)
            foreach (GameObject button in elementalTargetButtons)
                button.SetActive(false);

        // Remove console message if no more targets are available
        bool moreTargetsAvailable = false;
        foreach (GameObject button in elementalTargetButtons)
            if (button.activeSelf)
                moreTargetsAvailable = true;
        if (!moreTargetsAvailable)
            console.HideConsole();

        // Allow packet to be submitted
        submitButton.SetActive(true);
        cancelButton.SetActive(true);
    }

    public void SelectRechargeButton(string rechargeType)
    {
        packet.rechargeType = rechargeType;

        console.HideConsole();
        foreach (GameObject button in rechargeButtons)
            button.SetActive(false);

        submitButton.SetActive(true);
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
        console.HideConsole();
    }

    public void SelectSubmit()
    {
        ResetScene();

        console.WriteSingleConsoleMessage("Waiting for enemy", true);

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
        foreach (GameObject button in wildButtons)
            button.SetActive(false);
        foreach (GameObject button in rechargeButtons)
            button.SetActive(false);
        foreach (GameObject button in hexButtons)
            button.SetActive(false);

        console.HideConsole();

        NewAction?.Invoke(true);
    }
}