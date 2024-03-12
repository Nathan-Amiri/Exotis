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
    [SerializeField] private TargetManager targetManager;

    [SerializeField] private GameObject passButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private GameObject submitButton;

    [SerializeField] private List<GameObject> wildButtons = new();
    [SerializeField] private List<GameObject> hexButtons = new();

    [SerializeField] private List<Button> potionButtons = new();

    // DYNAMIC:
    private RelayPacket packet;
    private IDelegationAction currentAction;

    public void RequestDelegation(IDelegationAction immediateAction = null) // Called by ECore
    {
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
        {
            if (immediateAction == null)
            {
                Debug.LogError("Immediate action is null");
                return;
            }

            console.WriteConsoleMessage("Activate " + immediateAction.ParentElemental.name + "'s " + immediateAction.Name + "?");

            passButton.SetActive(true);
            immediateAction.OnNewActionNeeded();

            return;
        }

        // Repopulation only requires a delegation if there are two allies on the Bench
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
        {
            console.WriteConsoleMessage("Choose a Benched Elemental to Swap into play");

            if (NetworkManager.Singleton.IsHost)
                targetManager.DisplayTargets(new List<int> { }, new List<int> { 4, 5 }, true);
            else
                targetManager.DisplayTargets(new List<int> { }, new List<int> { 6, 7 }, true);


            return;
        }

        if (Clock.CurrentRoundState == Clock.RoundState.Timescale)
            console.WriteConsoleMessage("Choose an action to use at " + Clock.CurrentTimescale + ":00 or later");
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            console.WriteConsoleMessage("Choose a counter action");
        else
        {
            string time = Clock.CurrentRoundState == Clock.RoundState.RoundStart ? "beginning" : "end";
            console.WriteConsoleMessage("Choose an action to use at the " + time + " of the round");
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

        // Set packet info
        packet.actionType = currentAction.ActionType;
        packet.casterSlot = SlotAssignment.GetSlot(currentAction.ParentElemental);

        // Reset before proceeding
        ResetScene();

        if (currentAction is Spell spell)
        {
            packet.name = spell.name;

            if (spell.IsWild && packet.wildTimescale == 0)
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteConsoleMessage("At what time?");

                wildButtons[0].SetActive(true);
                wildButtons[1].SetActive(Clock.CurrentTimescale >= 5);

                return;
            }
            else if (spell.name == "Hex" && packet.hexType == null) // *Hex
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteConsoleMessage("Choose an effect");
                foreach (GameObject button in hexButtons)
                    button.SetActive(true);

                return;
            }
            else if (spell.name == "Landslide") // *Landslide
            {
                ResetScene(); //.consider making all similarly targeted traits use this code, making a separate method

                // Target all available targets in play
                List<int> landslideSlots = new();

                Dictionary<string, int> targetSlots = SlotAssignment.GetSlotDesignations(packet.casterSlot);

                if (CheckTargetAvailable(targetSlots["allySlot"]))
                    landslideSlots.Add(targetSlots["allySlot"]);
                if (CheckTargetAvailable(targetSlots["enemy1Slot"]))
                    landslideSlots.Add(targetSlots["enemy1Slot"]);
                if (CheckTargetAvailable(targetSlots["enemy2Slot"]))
                    landslideSlots.Add(targetSlots["enemy2Slot"]);

                packet.targetSlots = landslideSlots.ToArray();

                cancelButton.SetActive(true);
                submitButton.SetActive(true);
            }
        }
        else
            spell = null;

        // Check if the action is untargeted
        if (currentAction.MaxTargets == 0)
        {
            console.ResetConsole();
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

            console.WriteConsoleMessage("No available targets. Choose a different action", null, ConsoleOutput);

            return;
        }

        // *Singe check if targets can swap. If not, "No available targets that can Swap. Choose a different action"

        // Turn on target buttons
        targetManager.DisplayTargets(new List<int> { packet.casterSlot }, availableTargetSlots, true);

        string message = action.MaxTargets == 1 ? "Choose a target" : "Choose target(s)";
        console.WriteConsoleMessage(message);
        cancelButton.SetActive(true);
    }

    public void SelectWildButton(int wildTimescale)
    {
        packet.wildTimescale = wildTimescale;

        SelectAction(currentAction);
    }
    public void SelectHexButton(string hexType) // *Hex
    {
        packet.hexType = hexType;

        SelectAction(currentAction);
    }

    public void ConsoleOutput()
    {
        // RequestDelegation can't be passed to Console as an OutputMethod due to its parameters
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
            targetManager.ResetAllTargets();

            console.ResetConsole();

            submitButton.SetActive(true);
            cancelButton.SetActive(true);
        }

        // Make Potion interactable if currentAction is a single-target damaging Spell
        potionButtons[packet.casterSlot].interactable = PotionInteractable();

        // Turn off unavailable target buttons
        targetManager.ResetCertainTargets(new List<int> { targetSlot });

        if (packet.targetSlots.Length == currentAction.MaxTargets)
            targetManager.ResetAllTargets();


        // Remove console message if no more targets are available
        if (!targetManager.AnyTargetsAvailable())
            console.ResetConsole();

        // Allow packet to be submitted
        submitButton.SetActive(true);
        cancelButton.SetActive(true);
    }

    private bool PotionInteractable()
    {
        // CurrentAction is null when repopulating
        if (currentAction == null)
            return false;

        if (!currentAction.ParentElemental.HasPotion)
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
        targetManager.ResetAllTargets();

        console.ResetConsole();
    }

    public void SelectSubmit()
    {
        ResetScene();

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
        if (target.DisengageStrength > 0)
            return false;

        return true;
    }

    private void ResetScene()
    {
        passButton.SetActive(false);
        cancelButton.SetActive(false);
        submitButton.SetActive(false);

        targetManager.ResetAllTargets();

        foreach (GameObject button in wildButtons)
            button.SetActive(false);
        foreach (GameObject button in hexButtons) // *Hex
            button.SetActive(false);

        console.ResetConsole();

        NewAction?.Invoke(true);
    }



    public GameObject consoleButton; //.temp for shortcut
    private void Update() //.add customizable shortcuts eventually, don't allow cancel and submit to have the same key press
    {
        // Else if ensures priority order, so that the shortcut button doesn't trigger multiple things at once

        if (Input.GetKeyDown(KeyCode.Space) && consoleButton.activeSelf)
            console.SelectConsoleButton();
        else if (Input.GetKeyDown(KeyCode.Space) && passButton.activeSelf)
            SelectPass();
        else if (Input.GetKeyDown(KeyCode.X) && cancelButton.activeSelf)
            SelectCancel();
        else if (Input.GetKeyDown(KeyCode.Space) && submitButton.activeSelf)
            SelectSubmit();
    }
}