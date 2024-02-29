using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private TargetManager targetManager;

    // DYNAMIC:
        // 0 = not waiting for any packets (e.g. while waiting on console button),
        // 1/2 = packets needed before proceeding down a logic path
    private int expectedPackets;

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


    dim all but user, consider removing target names from console message. Maybe that means text can be bigger?
    do the same for delegation core when targeting

    is Freeze disengaging at counter speed okay? Is counter speed disengaging gonna cause problems?
    reword Freeze. Cancels Spell, Spell can be recast at the end of next round

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

        if (packet.hexType != string.Empty) // *Hex
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
        RequestDelegation(true, true);
    }

    private void RequestDelegation(bool needAllyDelegation, bool needEnemyDelegation)
    {
        expectedPackets = needAllyDelegation && needEnemyDelegation ? 2 : 1;

        if (needAllyDelegation)
            delegationCore.RequestDelegation();
        else if (enemyPacketQueue.Count > 0)
            ReceiveDelegation(GetNextEnemyPacketFromQueue());
        else
            console.WriteConsoleMessage("Waiting for enemy");
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

        // If expecting 0 (must be enemy packet)
        if (expectedPackets == 0)
        {
            // Wait until packet is needed
            enemyPacketQueue.Add(packet);
            return;
        }
        else if (expectedPackets == 1) // If expecting 1
        {
            singlePacket = packet;
            // Proceed down the correct logic path below
        }
        else if (isAllyPacket) // If expecting 2 and allyPacket
        {
            allyPacket = packet;

            // If there's an enemy packet in queue
            if (enemyPacketQueue.Count > 0)
            {
                enemyPacket = GetNextEnemyPacketFromQueue();
                // Proceed down the correct logic path below
            }
            else // Wait for enemy packet
                return;
        }
        else // If expecting 2 and enemyPacket
        {
            // If ally packet exists
            if (allyPacket.actionType != null)
            {
                enemyPacket = packet;
                // Proceed down the correct logic path below
            }
            else // Wait for ally packet
            {
                enemyPacketQueue.Add(packet);
                return;
            }
        }

        // Reset expectedPackets in case enemy delegations arrive before they're needed
        expectedPackets = 0;

        // Proceed down the corrent logic path
        if (Clock.CurrentRoundState == Clock.RoundState.Immediate)
            Immediate();
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
            Repopulation();
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            if (singlePacket.actionType != null)
                WriteCounterActionMessage(singlePacket);
            else
                CounterTiebreaker(allyCounterPacket, enemyCounterPacket);
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
        // Console rules:
        // *Pre-action message comparisons will use upper and lower text
        // *Post-comparison action messages and trait action messages will display action messages one at a time
        // *All other messages will be on one line

        // Get packet names
        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];

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
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will activate their Gems", null, GemEffect);
        }

        // If only one player activated a Gem
        else if (allyPacket.actionType == "gem")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot }, new List<int> { }, false);
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("Your " + allyCaster.name + "will activate its Gem", null, GemEffect);
        }
        else if (enemyPacket.actionType == "gem")
        {
            targetManager.DisplayTargets(new List<int> { enemyPacket.casterSlot }, new List<int> { }, false);
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
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { allyPacket.targetSlots[0], enemyPacket.targetSlots[0] }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will Retreat", null, RetreatEffect);
        }

        // If only one player Retreated
        else if (allyPacket.actionType == "retreat")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot }, new List<int> { allyPacket.targetSlots[0] }, false);
            IsolateFasterPacket(allyPacket);
            console.WriteConsoleMessage("Your " + allyCaster.name + " will Retreat", null, RetreatEffect);
        }
        else if (enemyPacket.actionType == "retreat")
        {
            targetManager.DisplayTargets(new List<int> { enemyPacket.casterSlot }, new List<int> { enemyPacket.targetSlots[0] }, false);
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
        targetManager.DisplayTargets(new List<int> { packet.casterSlot }, packet.targetSlots.ToList(), false);

        Elemental casterElemental = SlotAssignment.Elementals[packet.casterSlot];

        string packetOwner = packet.player == 0 == NetworkManager.Singleton.IsHost ? "Your" : "The enemy's";
        string caster = packetOwner + " " + casterElemental.name;

        string spellOrTraitName = packet.actionType == "spell" ? packet.name : casterElemental.trait.Name;

        if (packet.name == "Hex") // *Hex
        {
            string hexEffect = "Slowing";
            if (packet.hexType == "poison")
                hexEffect = "Poisoning";
            else if (packet.hexType == "weaken")
                hexEffect = "Weakening";

            return caster + " will cast Hex, " + hexEffect + " the target";
        }
        else if (packet.potion)
            return caster + " will drink its Potion, then cast " + packet.name;
        else
            return caster + " will cast " + spellOrTraitName;
    }

    public void CheckForCounter()
    {
        targetManager.ResetAllTargets();

        //.if recasting, SpellEffect() and return

        // Switch to counter roundState before calling CheckForAvailableActions
        clock.NewRoundState(Clock.RoundState.Counter);

        // If single counter
        if (singlePacket.actionType != null)
        {
            bool isAllyPacket = singlePacket.player == 0 == NetworkManager.Singleton.IsHost;

            // If countering player has no available actions, write to console and prepare SpellEffect
            if (!CheckForAvailableActions(singlePacket.player))
            {
                // Switch back from counter roundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                string packetOwner = isAllyPacket ? "You have" : "The enemy has";
                console.WriteConsoleMessage(packetOwner + " no available counter actions", null, SpellEffect);
            }
            else // If counter action is available, request counter delegation
                RequestDelegation(isAllyPacket, !isAllyPacket);
        }
        else // If multiple counter
        {
            bool allyCounterAvailable = CheckForAvailableActions(allyPacket.player);
            bool enemyCounterAvailable = CheckForAvailableActions(enemyPacket.player);

            if (!allyCounterAvailable && !enemyCounterAvailable)
            {
                // Switch back from counter roundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                console.WriteConsoleMessage("Neither player has available counter actions", null, SpellEffect);
            }
            else
                RequestDelegation(allyCounterAvailable, enemyCounterAvailable);
        }
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

    private void AutoPass(bool passAlly)
    {
        // Use the player number of the passing player
        RelayPacket passPacket = new()
        {
            player = passAlly == NetworkManager.Singleton.IsHost ? 0 : 1,
            actionType = "pass"
        };

        ReceiveDelegation(passPacket);
    }

    private bool CheckForAvailableActions(int player)
    {
        return true;
    }

    private bool CheckForGameOver()
    {
        //.eliminate elementals below 0 health

        return false;
    }

    private void ResetPackets()
    {
        singlePacket = default;
        allyPacket = default;
        enemyPacket = default;
    }
}