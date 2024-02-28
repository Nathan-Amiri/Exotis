using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.ShaderData;
using static UnityEngine.GraphicsBuffer;

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
    [SerializeField] private SlotButtons slotButtons;

    // DYNAMIC:
    private bool expectingSinglePacket;

    private RelayPacket singlePacket;
    private RelayPacket allyPacket;
    private RelayPacket enemyPacket;

    private readonly List<RelayPacket> enemyPacketQueue = new();

    private RelayPacket allyCounterPacket;
    private RelayPacket enemyCounterPacket;

    /*
        Todo:

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
        string debugMessage = "player[" + packet.player + "], " + packet.actionType;

        if (packet.actionType == "pass")
        {
            Debug.Log(debugMessage);
            return;
        }

        debugMessage += ", caster[" + packet.casterSlot + "]";

        if (packet.targetSlots.Length > 0)
        {
            string plural = packet.targetSlots.Length == 1 ? string.Empty : "s";
            debugMessage += ", target" + plural + "[" + packet.targetSlots[0];
            for (int i = 1; i < packet.targetSlots.Length; i++)
                debugMessage += ", " + packet.targetSlots[i];
            debugMessage += "]";
        }

        if (packet.actionType != "spell")
        {
            Debug.Log(debugMessage);
            return;
        }

        if (packet.name != string.Empty)
            debugMessage += ", " + packet.name.ToLower();

        if (packet.wildTimeScale != 0)
            debugMessage += ", wild[" + packet.wildTimeScale + "]";

        if (packet.rechargeType != string.Empty)
            debugMessage += "[" + packet.rechargeType + "]";

        if (packet.hexType != string.Empty)
            debugMessage += "[" + packet.hexType + "]";

        if (packet.potion)
            debugMessage += ", potion";

        if (packet.frenzy)
            debugMessage += ", frenzy";

        Debug.Log(debugMessage);
    }

    public void RoundStart() // Called by Setup
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
        NeedDelegation(true, true);
    }

    private void NeedDelegation(bool needAllyDelegation, bool needEnemyDelegation)
    {
        expectingSinglePacket = !(needAllyDelegation && needEnemyDelegation);

        if (needAllyDelegation)
            delegationCore.RequestDelegation();
        else if (enemyPacketQueue.Count > 0)
            ReceiveDelegation(GetNextEnemyPacketFromQueue());
    }

    private RelayPacket GetNextEnemyPacketFromQueue()
    {
        RelayPacket savedEnemyPacket = enemyPacketQueue[0];
        enemyPacketQueue.RemoveAt(0);
        return savedEnemyPacket;
    }

    public void ReceiveDelegation(RelayPacket packet) // Called by RelayCore
    {
        DebugPacket(packet);





        bool isAllyPacket = packet.player == 0 == NetworkManager.Singleton.IsHost;

        // If expecting single
        if (expectingSinglePacket)
        {
            singlePacket = packet;
            //.logic path
        }
        else if (isAllyPacket) // If expecting multiple and isAllyPacket
        {
            allyPacket = packet;

            // If enemy packet in queue
            if (enemyPacketQueue.Count > 0)
            {
                enemyPacket = GetNextEnemyPacketFromQueue();
                //.logic path
            }
            else // Wait for enemy packet
            {
                //.waiting for enemy message
                return;
            }
        }
        else // If expecting multiple and isEnemyPacket
        {
            // If ally packet exists
            if (allyPacket.actionType != null)
            {
                enemyPacket = packet;
                //.logic path
            }
            else // Wait for ally packet
            {
                enemyPacketQueue.Add(packet);
                return;
            }
        }










        //// If enemy and enemy packet already exists, save to queue
        //if (packet.player == 0 != NetworkManager.Singleton.IsHost && enemyPacket.actionType == null)
        //{
        //    enemyPacketQueue.Add(packet);
        //    return;
        //}

        //// Cache packets
        //if (expectingSinglePacket)
        //    singlePacket = packet;
        //else if (packet.player == 0 == NetworkManager.Singleton.IsHost)
        //    allyPacket = packet;
        //else
        //    enemyPacket = packet;

        //// Return if expecting multiple packets, but only one has been received
        //if (!expectingSinglePacket && (allyPacket.actionType == null || enemyPacket.actionType == null))
        //    return;

        // Proceed down the corrent logic path
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
            Immediate();
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
            Repopulation();
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            if (expectingSinglePacket)
                WriteCounterActionMessage(packet);
            else
                CounterTiebreaker(allyCounterPacket, enemyCounterPacket);
        }
        else
            TieBreaker();
    }

    private void AutoPass()
    {
        RelayPacket passPacket = new()
        {
            player = NetworkManager.Singleton.IsHost ? 0 : 1,
            actionType = "pass"
        };

        ReceiveDelegation(passPacket);
    }

    // Logic paths:
    private void Immediate()
    {

    }

    private void TieBreaker()
    {
        // Console rules:
        // *Pre-action message comparisons will use upper and lower text
        // *Post-comparison action messages and trait action messages will display action messages one at a time
        // *All other messages will be on one line

        // Get packet names
        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        List<string> allyTargetNames = GetTargetNamesFromPacket(allyPacket);
        List<string> enemyTargetNames = GetTargetNamesFromPacket(enemyPacket);

        //. figure out how to get timescales. (i.e. 7:00)
        int allyTimeScale = GetTimeScale(allyPacket);
        int enemyTimeScale = GetTimeScale(enemyPacket);

        // If both players passed
        if (allyPacket.actionType == "pass" && enemyPacket.actionType == "pass")
        {
            ResetPackets();

            if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
                console.WriteConsoleMessage("Both players have passed. Round will end", null, RoundEnd);
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
            slotButtons.TurnOnSlotButtons(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will activate their Gems", null, GemEffect);
        }

        // If only one player activated a Gem
        else if (allyPacket.actionType == "gem")
        {
            slotButtons.TurnOnSlotButtons(new List<int> { allyPacket.casterSlot }, false);
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("Your " + allyCaster.name + "will activate its Gem", null, GemEffect);
        }
        else if (enemyPacket.actionType == "gem")
        {
            slotButtons.TurnOnSlotButtons(new List<int> { enemyPacket.casterSlot }, false);
            IsolateFasterPacket(enemyPacket);
            console.WriteConsoleMessage("The enemy's " + enemyCaster.name + "will activate its Gem", null, GemEffect);
        }

        // If both players cast a Trait
        else if (allyPacket.actionType == "trait" && enemyPacket.actionType == "trait")
            WriteActionMessage();

        // If only one player cast a Trait
        else if (allyPacket.actionType == "trait")
        {
            IsolateFasterPacket(allyPacket);
            WriteActionMessage();
        }
        else if (enemyPacket.actionType == "trait")
        {
            IsolateFasterPacket(enemyPacket);
            WriteActionMessage();
        }

        // If both players Retreated
        else if (allyPacket.actionType == "retreat" && enemyPacket.actionType == "retreat")
        {
            slotButtons.TurnOnSlotButtons(new List<int> { allyPacket.targetSlots[0], enemyPacket.targetSlots[0] }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will Retreat", null, RetreatEffect);
        }

        // If only one player Retreated
        else if (allyPacket.actionType == "retreat")
        {
            slotButtons.TurnOnSlotButtons(new List<int> { allyPacket.targetSlots[0] }, false);
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("Your " + allyCaster.name + " will Retreat", null, RetreatEffect);
        }
        else if (enemyPacket.actionType == "retreat")
        {
            slotButtons.TurnOnSlotButtons(new List<int> { enemyPacket.targetSlots[0] }, false);
            IsolateFasterPacket(enemyPacket);
            console.WriteConsoleMessage("The enemy's " + enemyCaster.name + " will Retreat", null, RetreatEffect);
        }

        // If only one player Passed
        else if (allyPacket.actionType == "pass")
        {
            IsolateFasterPacket(enemyPacket);
            console.WriteConsoleMessage("You have passed", "The enemy will act at " + enemyTimeScale + ":00", WriteActionMessage);
        }
        else if (enemyPacket.actionType == "pass")
        {
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("You will act at " + allyTimeScale + ":00", "The enemy has passed", WriteActionMessage);
        }

        // If one player declared a sooner timescale
        else if (allyTimeScale > enemyTimeScale)
        {
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("You will act first at " + allyTimeScale + ":00", "The enemy planned to act at " + enemyTimeScale + ":00", WriteActionMessage);
        }
        else if (enemyTimeScale > allyTimeScale)
        {
            IsolateFasterPacket(enemyPacket);
            console.WriteConsoleMessage("You planned to act at " + allyTimeScale + ":00", "The enemy will act first at " + enemyTimeScale + ":00", WriteActionMessage);
        }

        // If timescales tied, use Elemental's MaxHealth to determine Elemental speed
        else if (allyCaster.MaxHealth < enemyCaster.MaxHealth)
        {
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                "Your " + allyCaster.name + " outsped the enemy's " + enemyCaster.name, null, WriteActionMessage);
        }
        else if (allyCaster.MaxHealth > enemyCaster.MaxHealth)
        {
            IsolateFasterPacket(enemyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                "The enemy's " + enemyCaster.name + " outsped your " + allyCaster.name, null, WriteActionMessage);
        }

        // If timescales and Elemental speeds tied
        else
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                "Your " + allyCaster.name + " tied with the enemy's " + enemyCaster.name, null, WriteActionMessage);
    }
    private int GetTimeScale(RelayPacket packet)
    {
        if (packet.actionType != "spell")
            return 0;

        if (packet.wildTimeScale != 0)
            return packet.wildTimeScale;

        List<Spell> casterSpells = SlotAssignment.Elementals[packet.casterSlot].spells;
        Spell castSpell = null;
        foreach (Spell spell in casterSpells)
            if (spell.Name == packet.name)
            {
                castSpell = spell;
                break;
            }
        if (castSpell == null)
            Debug.LogError("Caster's Spell not found");

        return castSpell.TimeScale;
    }
    private void IsolateFasterPacket(RelayPacket fasterPacket)
    {
        ResetPackets();
        singlePacket = fasterPacket;
    }

    public void WriteActionMessage()
    {
        // If two packets must be written, write ally and prepare to write enemy
        if (singlePacket.actionType == null)
            console.WriteConsoleMessage(GenerateActionMessage(allyPacket), null, WriteEnemyMessage);
        // If a single packet must be written, write it and prepare TraitEffect/CheckForCounter depending on actionType
        else
        {
            Console.OutputMethod outputMethod = singlePacket.actionType == "trait" ? TraitEffect : CheckForCounter;
            console.WriteConsoleMessage(GenerateActionMessage(singlePacket), null, outputMethod);
        }
    }
    public void WriteEnemyMessage()
    {
        // Write enemy packet and prepare TraitEffect/CheckForCounter depending on actionType
        Console.OutputMethod outputMethod = enemyPacket.actionType == "trait" ? TraitEffect : CheckForCounter;
        console.WriteConsoleMessage(GenerateActionMessage(enemyPacket), null, outputMethod, true);
    }

    private string GenerateActionMessage(RelayPacket packet)
    {
        if (packet.name == "Landslide")
        {
            // Get non-caster slots in play
            List<int> targets = new() { 0, 1, 2, 3 };
            targets.Remove(packet.casterSlot);

            // Remove unavailable targets
            foreach (int target in targets)
            {
                Elemental targetElemental = SlotAssignment.Elementals[target];
                if (targetElemental == null || targetElemental.isDisengaged)
                    targets.Remove(target);
            }

            slotButtons.TurnOnSlotButtons(targets, false);
        }
        else
            slotButtons.TurnOnSlotButtons(packet.targetSlots.ToList(), false);


        string message;

        Elemental casterElemental = SlotAssignment.Elementals[packet.casterSlot];

        string owner = packet.player == 0 == NetworkManager.Singleton.IsHost ? "Your" : "The enemy's";
        string caster = owner + " " + casterElemental.name;

        List<string> targetNames = GetTargetNamesFromPacket(packet);
        // If any targets are the caster, write "itself" instead
        for (int i = 0; i < targetNames.Count; i++)
            if (packet.targetSlots[i] == packet.casterSlot)
                targetNames[i] = "itself";

        string spellOrTraitName = packet.actionType == "spell" ? packet.name : casterElemental.trait.Name;

        if (packet.name == "Recharge")
        {
            string rechargeEffect = packet.rechargeType == "heal" ? "heal " + targetNames[1] + " 1" : "give " + targetNames[1] + " a Spark";
            message = caster + " will cast Recharge on " + targetNames[0] + ", and will " + rechargeEffect;
        }
        else if (packet.name == "Hex")
        {
            string hexEffect = "Slowing";
            if (packet.hexType == "poison")
                hexEffect = "Poisoning";
            else if (packet.hexType == "weaken")
                hexEffect = "Weakening";

            message = caster + " will cast Hex on " + targetNames[0] + ", " + hexEffect + " them";
        }
        else if (packet.potion)
            message = caster + " will drink its Potion, then cast " + packet.name + " on " + targetNames[0];
        else
        {
            message = caster + " will cast " + spellOrTraitName;

            if (packet.targetSlots.Length == 1)
                message += " on " + targetNames[0];
            else if (packet.targetSlots.Length == 2)
                message += " on " + targetNames[0] + " and " + targetNames[1];
            else if (packet.targetSlots.Length == 3)
                message += " on " + targetNames[0] + ", " + targetNames[1] + ", and " + targetNames[2];
        }

        return message;
    }

    public void CheckForCounter()
    {
        //.if recasting, SpellEffect() and return

        //if (!CheckForAvailableActions(singlePacket.player))
        //{
        //    console.WriteConsoleMessage("")
        //}

        //.no need for delegation if enemy can't act since teams are open
    }

    private void CounterTiebreaker(RelayPacket allyCounterPacket, RelayPacket enemyCounterPacket)
    {

    }

    private void WriteCounterActionMessage(RelayPacket counterPacket)
    {

    }
    public void WriteEnemyCounterActionMessage()
    {

    }

    // Effect methods:
    private void GemEffect()
    {
        Debug.Log("GemEffect");
    }

    private void RetreatEffect()
    {
        Debug.Log("RetreatEffect");
    }

    private void TraitEffect()
    {
        Debug.Log("TraitEffect");
    }

    private void SpellEffect()
    {
        Debug.Log("SpellEffect");
    }

    private void SparkEffect()
    {
        Debug.Log("SparkEffect");
    }

    private void RoundEnd()
    {
        Debug.Log("RoundEnd");
    }

    private void Repopulation()
    {
        Debug.Log("Repopulation");
    }

    // Misc methods:
    private bool CheckForAvailableActions(int player)
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

    private void ResetPackets()
    {
        singlePacket = default;
        allyPacket = default;
        enemyPacket = default;
    }
}