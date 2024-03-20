using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static ElementalInfo;

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
    [SerializeField] private ExecutionCore executionCore;
    [SerializeField] private SlotAssignment slotAssignment;
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

        packet.actionType = currentAction.ActionType;
        packet.casterSlot = slotAssignment.GetSlot(currentAction.ParentElemental);

        ResetScene();

        if (currentAction is Spell spell)
        {
            packet.name = spell.Name;

            if (spell.IsWild && packet.wildTimescale == 0)
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteConsoleMessage("At what time?");

                wildButtons[0].SetActive(true);
                wildButtons[1].SetActive(Clock.CurrentTimescale >= 5);

                return;
            }
            else if (spell.Name == "Landslide") // *Landslide
            {
                ResetScene();

                List<int> targets = new();
                for (int i = 0; i < 4; i++)
                    if (i != packet.casterSlot && slotAssignment.CheckTargetAvailable(i))
                        targets.Add(i);

                packet.targetSlots = targets.ToArray();

                cancelButton.SetActive(true);
                submitButton.SetActive(true);
            }
            else if (spell.Name == "Numbing Cold") // *Numbing Cold
            {
                List<int> targets = new();
                for (int i = 0; i < 4; i++)
                    if (slotAssignment.CheckTargetAvailable(i) && slotAssignment.Elementals[i].Speed < currentAction.ParentElemental.Speed)
                        targets.Add(i);

                if (targets.Count == 0)
                {
                    WriteFailMessage("No available targets slower than the caster. Choose a different action");
                    return;
                }

                targetManager.DisplayTargets(new List<int> { packet.casterSlot }, targets, true);

                console.WriteConsoleMessage("Choose a target");
                cancelButton.SetActive(true);

                return;
            }
            else if (spell.Name == "Hex" && packet.hexType == null) // *Hex
            {
                ResetScene();

                cancelButton.SetActive(true);
                console.WriteConsoleMessage("Choose an effect");
                foreach (GameObject button in hexButtons)
                    button.SetActive(true);

                return;
            }
            else if (spell.Name == "Mirage" && !currentAction.ParentElemental.CanSwapOut()) // *Mirage
            {
                WriteFailMessage("The caster is unable to Swap. Choose a different action");
                return;
            }
            else if (spell.name == "Block" && !spell.readyForRecast && !executionCore.GetCounteringSpell().IsDamaging) // *Block
            {
                WriteFailMessage("Block cannot counter a non-damaging Spell. Choose a different action");
                return;
            }
            else if (spell.Name == "Flurry" && !currentAction.ParentElemental.AllyCanSwapOut()) // *Flurry
            {
                WriteFailMessage("The caster's Ally is unable to Swap. Choose a different action");
                return;
            }
            else if (spell.Name == "Allure" && !executionCore.AllureAvailable(currentAction.ParentElemental)) // *Allure
            {
                WriteFailMessage("The caster's ally is not being targeted. Choose a different action");
                return;
            }
        }
        else
            spell = null;

        int maxTargets = GetMaxTargets(action);

        // If the action is untargeted
        if (maxTargets == 0)
        {
            console.ResetConsole();
            cancelButton.SetActive(true);
            submitButton.SetActive(true);

            return;
        }

        List<int> availableTargetSlots = new();

        bool recast = spell != null && spell.readyForRecast;

        if (currentAction.CanTargetSelf || recast)
            availableTargetSlots.Add(packet.casterSlot);

        int allySlot = packet.casterSlot % 2 == 0 ? packet.casterSlot + 1 : packet.casterSlot - 1;
        if ((currentAction.CanTargetAlly || recast) && slotAssignment.CheckTargetAvailable(allySlot))
            availableTargetSlots.Add(allySlot);

        int a = NetworkManager.Singleton.IsHost ? 0 : 2;

        if (currentAction.CanTargetEnemy || recast)
        {
            if (slotAssignment.CheckTargetAvailable(2 - a))
                availableTargetSlots.Add(2 - a);
            if (slotAssignment.CheckTargetAvailable(3 - a))
                availableTargetSlots.Add(3 - a);
        }

        if (currentAction.CanTargetBenchedAlly)
        {
            if (slotAssignment.CheckTargetAvailable(4 + a))
                availableTargetSlots.Add(4 + a);
            if (slotAssignment.CheckTargetAvailable(5 + a))
                availableTargetSlots.Add(5 + a);
        }

        if (availableTargetSlots.Count == 0)
        {
            WriteFailMessage("No available targets. Choose a different action");
            return;
        }

        // *Singe check if targets can swap. If not, "No available targets that can Swap. Choose a different action"

        targetManager.DisplayTargets(new List<int> { packet.casterSlot }, availableTargetSlots, true);

        string message = maxTargets == 1 ? "Choose a target" : "Choose target(s)";
        console.WriteConsoleMessage(message);
        cancelButton.SetActive(true);
    }
    private void WriteFailMessage(string message)
    {
        packet = default;

        console.WriteConsoleMessage(message, null, ConsoleOutput);

        return;
    }
    private int GetMaxTargets(IDelegationAction action)
    {
        if (action is Spell spell && spell.readyForRecast)
            return spell.maxRecastTargets;

        return action.MaxTargets;
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
        else if (packet.name == "Take Flight" && packet.targetSlots.Length == 1) // *Take Flight
        {
            List<int> availableSwapTargets = new();

            int a = NetworkManager.Singleton.IsHost ? 0 : 2;
            List<int> benchedSlots = new() { 4 + a, 5 + a };

            foreach (int slot in benchedSlots)
            {
                Elemental benchedElemental = slotAssignment.Elementals[slot];
                if (benchedElemental != null && !benchedElemental.cannotSwapIn)
                    availableSwapTargets.Add(slot);
            }

            if (availableSwapTargets.Count == 2)
            {
                ResetScene();

                targetManager.DisplayTargets(new List<int> { packet.casterSlot }, availableSwapTargets, true);

                return;
            }
            else // 1 or 0
                packet.targetSlots = new int[] { targetSlot, availableSwapTargets[0] };
        }

        potionButtons[packet.casterSlot].interactable = PotionInteractable();

        // Turn off unavailable target buttons
        targetManager.ResetCertainTargets(new List<int> { targetSlot });

        if (packet.targetSlots.Length >= GetMaxTargets(currentAction))
            targetManager.ResetAllTargets();


        if (!targetManager.AnyTargetsAvailable())
            console.ResetConsole();

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

        if (currentAction is not Spell spell)
            return false;

        bool spellIsDamaging = spell.readyForRecast ? spell.recastIsDamaging : spell.IsDamaging;
        if (!spellIsDamaging)
            return false;

        if (packet.targetSlots.Length != 1)
            return false;

        return true;
    }

    public void SelectPotion()
    {
        packet.potion = true;

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