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

    private readonly List<RelayPacket> enemyPacketQueue = new();

    private RelayPacket singlePacket;
    private RelayPacket allyPacket;
    private RelayPacket enemyPacket;

        // Spells and Traits save packets before occurring in case additional delegations are needed (such as counters)
    private RelayPacket savedSinglePacket;
    private RelayPacket savedAllyPacket;
    private RelayPacket savedEnemyPacket;

    /*
        Todo:
    
    gemeffect
    retreateffect
    traiteffect
    spelleffect
    sparkeffect

    roundend
    repopulation
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

        if (packet.hexType != string.Empty) // *Hex
            debugMessage += "[" + packet.hexType + "]";

        if (packet.potion)
            debugMessage += ", potion";

        if (packet.frenzy)
            debugMessage += ", frenzy";

        Debug.Log(debugMessage);
    }


    //.not sure what to call these methods yet:
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


    // Packet methods:
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

        DebugPacket(packet);

        // Reset expectedPackets before proceeding in case enemy delegations arrive before they're needed
        expectedPackets = 0;

        if (singlePacket.actionType == null)
            TieBreaker();
        else
            WriteActionMessage();
    }


    // Tiebreaker methods:
    private void TieBreaker()
    {
        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];

        int allyTimeScale = GetTimeScale(allyPacket);
        int enemyTimeScale = GetTimeScale(enemyPacket);

        // If both players passed
        if (allyPacket.actionType == "pass" && enemyPacket.actionType == "pass")
            DoublePass();

        // If both players Retreated
        else if (allyPacket.actionType == "retreat" && enemyPacket.actionType == "retreat")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { allyPacket.targetSlots[0], enemyPacket.targetSlots[0] }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will Retreat", null, RetreatEffect);
        }

        // Check if either player Retreated. If so, isolate that player's packet and write action message
        if (CheckForActionType("retreat"))
            return;

        // If both players activated Gems
        else if (allyPacket.actionType == "gem" && enemyPacket.actionType == "gem")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will activate their Gems", null, GemEffect);
        }

        // Check if either player activated a Gem. If so, isolate that player's packet and write action message
        else if (CheckForActionType("gem"))
            return;

        // If both players sent a Spark
        else if (allyPacket.actionType == "spark" && enemyPacket.actionType == "spark")
            WriteActionMessage();

        // Check if either player sent a Spark. If so, isolate that player's packet and write action message
        else if (CheckForActionType("spark"))
            return;

        // If both players cast a Trait
        else if (allyPacket.actionType == "trait" && enemyPacket.actionType == "trait")
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will cast their Traits", null, WriteActionMessage);

        // Check if either player cast a Trait. If so, isolate that player's packet and write action message
        else if (CheckForActionType("trait"))
            return;

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
    private void DoublePass()
    {
        //// If both players passed

        //ResetPackets();

        //if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        //{
        //    //.what do I do here?
        //}
        //else if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
        //    console.WriteConsoleMessage("Both players have passed. Round will end", null, RoundEnd);

        //// If both players pass at RoundStart/End, continue without writing to console
        //else if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
        //{
        //    clock.NewRoundState(Clock.RoundState.TimeScale);
        //    NewCycle();
        //}
        ////.else if roundend, continue without writing to console--but what happens next?

    }
    private bool CheckForActionType(string actionType)
    {
        // Check if either single packet is of actionType. If so, write the action message for it
        // If both packets are of actionType, this method won't be called

        if (allyPacket.actionType == actionType)
        {
            IsolateFasterPacket(enemyPacket);
            WriteActionMessage();
            return true;
        }
        else if (enemyPacket.actionType == actionType)
        {
            IsolateFasterPacket(enemyPacket);
            WriteActionMessage();
            return true;
        }

        return false;
    }
    private void IsolateFasterPacket(RelayPacket newSinglePacket)
    {
        ResetPackets();
        singlePacket = newSinglePacket;
    }


    // ActionMessage methods:
    public void WriteActionMessage()
    {
        // If one packet must be written, write it and prepare the correct output method
        if (singlePacket.actionType != null)
        {
            (string, Console.OutputMethod) messageAndOutput = GenerateActionMessage(singlePacket);
            console.WriteConsoleMessage(messageAndOutput.Item1, null, messageAndOutput.Item2);
        }
        // If two packets must be written, write ally message and prepare to write enemy message
        else
        {
            string message = GenerateActionMessage(allyPacket).Item1;
            console.WriteConsoleMessage(message, null, WriteEnemyActionMessage);
        }
    }
    public void WriteEnemyActionMessage()
    {
        // Write enemy message and prepare the correct output method
        (string, Console.OutputMethod) messageAndOutput = GenerateActionMessage(enemyPacket);
        console.WriteConsoleMessage(messageAndOutput.Item1, null, messageAndOutput.Item2);
    }

    private (string, Console.OutputMethod) GenerateActionMessage(RelayPacket packet)
    {
        targetManager.DisplayTargets(new List<int> { packet.casterSlot }, packet.targetSlots.ToList(), false);

        Elemental casterElemental = SlotAssignment.Elementals[packet.casterSlot];

        string packetOwner = packet.player == 0 == NetworkManager.Singleton.IsHost ? "Your" : "The enemy's";
        string caster = packetOwner + " " + casterElemental.name;


        if (packet.actionType == "pass")
        {
            //.figure out where all it might need to go next (if only one player passed and it's double action message, this code won't run, so don't worry about that!

            //.from original singlecounter:
            //// If you pass, immediately write spell message. If enemy passes, first tell player that enemy has passed
            //if (singlePacket.actionType == "pass")
            //{
            //    if (isCounteringPlayer)
            //        WriteSpellMessage();
            //    else
            //        console.WriteConsoleMessage("The enemy has passed", null, WriteSpellMessage);

            //    return;
            //}
        }

        if (packet.actionType == "retreat")
            return (caster + " will Retreat", RetreatEffect);

        if (packet.actionType == "gem")
            return (caster + " will activate its Gem", GemEffect);

        if (packet.actionType == "spark")
            return (caster + " will send its Spark at the target", SparkEffect);

        if (packet.actionType == "trait")
            return (caster + " will cast " + casterElemental.trait.Name, TraitEffect);

        if (packet.name == "Hex") // *Hex
        {
            string hexEffect = "Slowing";
            if (packet.hexType == "poison")
                hexEffect = "Poisoning";
            else if (packet.hexType == "weaken")
                hexEffect = "Weakening";

            return (caster + " will cast Hex, " + hexEffect + " the target", CheckForCounter);
        }

        if (packet.potion)
            return (caster + " will drink its Potion, then cast " + packet.name, CheckForCounter);

        return (caster + " will cast " + packet.name, CheckForCounter);
    }


    // CheckForCounter methods:
    public void CheckForCounter()
    {
        targetManager.ResetAllTargets();

        // If it's already counter time, packets are already saved
        if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            // Save packets before requesting new ones
            savedSinglePacket = singlePacket;
            savedAllyPacket = allyPacket;
            savedEnemyPacket = enemyPacket;
            ResetPackets();
        }
        else // Switch to counter roundState before calling CheckForAvailableActions
            clock.NewRoundState(Clock.RoundState.Counter);

        // If single counter
        if (savedSinglePacket.actionType != null)
        {
            bool isCounteringPlayer = savedSinglePacket.player == 0 != NetworkManager.Singleton.IsHost;

            // If countering player has no available actions, write to console and prepare SpellEffect
            if (!CheckForAvailableActions(savedSinglePacket.player))
            {
                // Switch back from counter roundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                string counteringPlayer = isCounteringPlayer ? "You have" : "The enemy has";
                console.WriteConsoleMessage(counteringPlayer + " no available counter actions", null, WriteSpellMessage);
            }
            else // If counter action is available, save packet and request counter delegation
                RequestDelegation(isCounteringPlayer, !isCounteringPlayer);
        }
        else // If multiple counter
        {
            bool allyCounterAvailable = CheckForAvailableActions(savedAllyPacket.player);
            bool enemyCounterAvailable = CheckForAvailableActions(savedEnemyPacket.player);

            if (!allyCounterAvailable && !enemyCounterAvailable)
            {
                // Switch back from counter roundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                console.WriteConsoleMessage("Neither player has available counter actions", null, WriteSpellMessage);
            }
            else if (allyCounterAvailable && enemyCounterAvailable)
                RequestDelegation(true, true);
            else if (allyCounterAvailable && !enemyCounterAvailable)
                // Inform the player that the enemy cannot counter before requesting ally delegation
                console.WriteConsoleMessage("The enemy has no available counter actions", null, EnemyCannotCounter);
            else // If only enemy counter available
                console.WriteConsoleMessage("You have no available counter actions", null, AllyCannotCounter);
        }
    }
    public void EnemyCannotCounter()
    {
        RequestDelegation(true, false);
    }
    public void AllyCannotCounter()
    {
        RequestDelegation(false, true);
    }


    // Effect methods:
    private void RetreatEffect()
    {
        Debug.Log("RetreatEffect");
    }

    private void GemEffect()
    {
        Debug.Log("GemEffect");
    }

    private void SparkEffect()
    {
        Debug.Log("SparkEffect");
    }

    private void TraitEffect()
    {
        Debug.Log("TraitEffect");
    }

    public void WriteSpellMessage()
    {
        Debug.Log("SpellMessage");
        //."caster's spell will occur" output method is spelleffect NOT USED FOR COUNTER, COUNTER JUMPS RIGHT TO SPELLEFFECT
    }
    public void SpellEffect()
    {
        Debug.Log("SpellEffect");
        //.IF COUNTER, RETURN TO CHECKFORCOUNTER SO THAT OTHER COUNTERS CAN OCCUR BEFORE THE SAVED SPELL DOES
    }


    //.not sure what to call these methods yet:
    private void RoundEnd()
    {
        Debug.Log("RoundEnd");
    }

    //private void Repopulation()
    //{
    //    Debug.Log("Repopulation");
    //}

    // Misc methods:

    //private void AutoPass(bool passAlly) //.do I need this method?
    //{
    //    // Use the player number of the passing player
    //    RelayPacket passPacket = new()
    //    {
    //        player = passAlly == NetworkManager.Singleton.IsHost ? 0 : 1,
    //        actionType = "pass"
    //    };

    //    ReceiveDelegation(passPacket);
    //}



    //.not sure what to call these methods yet:
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