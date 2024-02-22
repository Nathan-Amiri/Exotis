using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExecutionCore : MonoBehaviour
{
    // After receiving the details of a player's action,
    // ExecutionCore determines the consequences of that action.

    // Core logic classes can only impact each other in one direction:
    // DelegationCore > RelayCore > ExecutionCore > DelegationCore

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;
    [SerializeField] private Clock clock;
    [SerializeField] private Console console;

    // DYNAMIC:
    private bool expectingSinglePacket;

    private RelayPacket singlePacket;
    private RelayPacket allyPacket;
    private RelayPacket enemyPacket;

    /*
        Todo:

    tiebreaker

    selectconsolebutton

    singlecounter
    countertiebreaker
    immediate (and immediate tiebreaker?)
    
    
    gemeffect
    retreateffect
    traiteffect
    spelleffect
    sparkeffect

    roundend
    repopulation (and repopulation tiebreaker?)
    roundstart
    
    checkforavailableactions
    checkforgameover

    */

    private void DebugPacket(RelayPacket packet)
    {
        string message = "player[" + packet.player + "], " + packet.actionType;

        if (packet.actionType == "pass")
        {
            Debug.Log(message);
            return;
        }

        message += ", caster[" + packet.casterSlot + "]";

        if (packet.targetSlots.Length > 0)
        {
            string plural = packet.targetSlots.Length == 1 ? string.Empty : "s";
            message += ", target" + plural + "[" + packet.targetSlots[0];
            for (int i = 1; i < packet.targetSlots.Length; i++)
                message += ", " + packet.targetSlots[i];
            message += "]";
        }

        if (packet.actionType != "spell")
        {
            Debug.Log(message);
            return;
        }

        if (packet.name != string.Empty)
            message += ", " + packet.name.ToLower();

        if (packet.wildTimeScale != 0)
            message += ", wild[" + packet.wildTimeScale + "]";

        if (packet.rechargeType != string.Empty)
            message += "[" + packet.rechargeType + "]";

        if (packet.hexType != string.Empty)
            message += "[" + packet.hexType + "]";

        if (packet.potion == true)
            message += ", potion";

        if (packet.frenzy == true)
            message += ", frenzy";

        Debug.Log(message);
    }

    // Called by Setup
    public void RoundStart()
    {
        //.delayed effects occur simultaneously and silently
        //.cycle text messages using preset order (see bible)

        NewCycle();
    }

    private void NewCycle()
    {
        //.if immediate available, do immediate things
        //.if no actions available, autopass and "You have no available actions"

        // Request roundstart/end/timescale delegation

        expectingSinglePacket = false;
        delegationCore.RequestDelegation();
    }

    public void ReceivePacket(RelayPacket packet) // Called by RelayCore
    {
        DebugPacket(packet);

        // Cache packets
        if (expectingSinglePacket)
            singlePacket = packet;
        else if (packet.player == 0 == NetworkManager.Singleton.IsHost)
            allyPacket = packet;
        else
            enemyPacket = packet;

        // Return if expecting multiple packets, but only one has been received
        if (!expectingSinglePacket && (allyPacket.actionType == null || enemyPacket.actionType == null))
            return;

        // Proceed down the corrent logic path
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
            Immediate();
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
            Repopulation();
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            if (expectingSinglePacket)
                SingleCounter();
            else
                CounterTieBreaker();
        }
        else
            TieBreaker();
    }

    // Logic paths:
    private void Immediate()
    {

    }

    private void TieBreaker()
    {
        //. display targets when appropriate

        // Get packet names

        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];

        List<string> allyTargetNames = GetTargetNamesFromPacket(allyPacket);
        List<string> enemyTargetNames = GetTargetNamesFromPacket(enemyPacket);

        //. figure out how to get timescales. (i.e. 7:00)
        int allyTimeScale = 0;
        int enemyTimeScale = 0;

        // If both players passed
        if (allyPacket.actionType == "pass" && enemyPacket.actionType == "pass")
        {
            ResetPackets();

            if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
            {
                console.WriteSingleConsoleMessage("Both players have passed. Round will end", false);
                //.ensure that consolebutton returns to the correct location

            }
            // If both players pass at RoundStart/End, continue without writing to console
            else if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
            {
                clock.NewRoundState(Clock.RoundState.TimeScale);
                NewCycle();
            }
            //.else if roundend, continue without writing to console--but what happens next?
        }
        // If both players activated Gems
        else if (allyPacket.actionType == "gem" && enemyPacket.actionType == "gem")
        {
            console.WriteMultipleConsoleMessage("Your " + allyCaster.name + " will activate its Gem", 
                "The enemy's " + enemyCaster.name + " will activate its Gem");
            //.ensure that consolebutton returns to the correct location
        }
        // If only one player activated a Gem
        else if (allyPacket.actionType == "gem")
        {
            console.WriteSingleConsoleMessage("Your " + allyCaster.name + "will activate its Gem", false);
            //.ensure that consolebutton returns to the correct location
        }
        else if (enemyPacket.actionType == "gem")
        {
            console.WriteSingleConsoleMessage("The enemy's " + enemyCaster.name + "will activate its Gem", false);
            //.ensure that consolebutton returns to the correct location
        }
        //.if traits, display action message immediately, no need for tiebreaker
        // If both players Retreated
        else if (allyPacket.actionType == "retreat" && enemyPacket.actionType == "retreat")
        {
            console.WriteMultipleConsoleMessage("Your " + allyCaster.name + " will Retreat, Swapping with " + allyTargetNames[1],
                "The enemy's " + enemyCaster.name + " will Retreat, Swapping with " + enemyTargetNames[1]);
            //.ensure that consolebutton returns to the correct location
        }
        // If only one player Retreated
        else if (allyPacket.actionType == "retreat")
        {
            console.WriteSingleConsoleMessage("Your " + allyCaster.name + " will Retreat, Swapping with " + allyTargetNames[1], false);
            //.ensure that consolebutton returns to the correct location
        }
        else if (enemyPacket.actionType == "retreat")
        {
            console.WriteSingleConsoleMessage("The enemy's " + enemyCaster.name + " will Retreat, Swapping with " + enemyTargetNames[1], false);
            //.ensure that consolebutton returns to the correct location
        }
        // If only one player Passed
        else if (allyPacket.actionType == "pass")
        {
            console.WriteMultipleConsoleMessage("You have passed", 
                "The enemy will act at " + enemyTimeScale);
            //.ensure that consolebutton returns to the correct location
        }
        else if (enemyPacket.actionType == "pass")
        {
            console.WriteMultipleConsoleMessage("You will act at " + allyTimeScale, 
                "The enemy has passed");
            //.ensure that consolebutton returns to the correct location
        }
        // If one player declared a sooner Timescale
        else if (allyTimeScale > enemyTimeScale)
        {
            console.WriteMultipleConsoleMessage("You will act first at " + allyTimeScale, 
                "The enemy planned to act at " + enemyTimeScale);
            //.ensure that consolebutton returns to the correct location
        }
        else if (enemyTimeScale > allyTimeScale)
        {
            console.WriteMultipleConsoleMessage("You planned to act at " + allyTimeScale,
                "The enemy will act first at " + enemyTimeScale);
            //.ensure that consolebutton returns to the correct location
        }
        // If Timescales tied, use Elemental's MaxHealth to determine Elemental speed
        else if (allyCaster.MaxHealth < enemyCaster.MaxHealth)
        {
            console.WriteSingleConsoleMessage("Both players planned to act at " + allyTimeScale + ". " +
                "Your " + allyCaster.name + " outsped the enemy's " + enemyCaster, false);
            //.ensure that consolebutton returns to the correct location
        }
        else if (allyCaster.MaxHealth > enemyCaster.MaxHealth)
        {
            console.WriteSingleConsoleMessage("Both players planned to act at " + allyTimeScale + ". " +
                "The enemy's " + enemyCaster.name + " outsped the your " + allyCaster, false);
            //.ensure that consolebutton returns to the correct location
        }
        // Timescales and Elemental speeds tied
        else
        {
            console.WriteSingleConsoleMessage("Both players planned to act at " + allyTimeScale + ". " +
                "Your " + allyCaster + " and the enemy's " + enemyCaster + " are the same speed, and will act simultaneously", false);
            //.ensure that consolebutton returns to the correct location
        }

        //.figure out action messages

        //.if I put code down here, I have to make sure everything above returns if it needs to!
    }

    private void SingleCounter()
    {

    }

    private void CounterTieBreaker()
    {

    }

    // Effect methods:
    private void GemEffect()
    {

    }

    private void RetreatEffect()
    {

    }

    private void TraitEffect()
    {

    }

    private void SpellEffect()
    {

    }

    private void SparkEffect()
    {

    }

    private void RoundEnd()
    {

    }

    private void Repopulation()
    {

    }

    // Misc methods:
    private bool CheckForAvailableActions()
    {
        return true;
    }

    private bool CheckForGameOver()
    {
        //.eliminate elementals below 0 health

        return false;
    }

    private List<string> GetTargetNamesFromPacket(RelayPacket packet)
    {
        List<string> targetNames = new();

        // If packet is default, targetSlots.Length = 0
        foreach (int targetSlot in packet.targetSlots)
            targetNames.Add(SlotAssignment.Elementals[targetSlot].name);

        return targetNames;
    }



    public void SelectConsoleButton()
    {

    }

    private void ResetPackets()
    {
        singlePacket = default;
        allyPacket = default;
        enemyPacket = default;
    }
}