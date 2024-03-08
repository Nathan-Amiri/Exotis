using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor.VersionControl;
using UnityEngine;
using static Unity.Networking.Transport.Utilities.ReliableUtility;

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

    // CONSTANT:
    private delegate void EffectDelegate(RelayPacket effectPacket);

    // DYNAMIC:
    // 0 = not waiting for any packets (e.g. while waiting on console button),
    // 1 or 2 = number of packets needed before executing packets
    private int expectedPackets;

    private readonly List<RelayPacket> enemyPacketQueue = new();

    private RelayPacket singlePacket;
    private RelayPacket allyPacket;
    private RelayPacket enemyPacket;

        // Spells cache their packets here before requesting new counter packets
        // Caster Elementals are cached in case casters Swap before their Spell occurs
        // No need to store messages as SparkInfos do, since casters can't be Eliminated at counter speed
    private (RelayPacket, Elemental) savedSinglePacket;
    private (RelayPacket, Elemental) savedAllyPacket;
    private (RelayPacket, Elemental) savedEnemyPacket;

    private readonly List<SparkInfo> flungSparks = new();

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

    // Packet methods:
    private void RequestPacket(bool needAllyPacket, bool needEnemyPacket)
    {
        expectedPackets = needAllyPacket && needEnemyPacket ? 2 : 1;

        if (needAllyPacket)
            delegationCore.RequestDelegation();
        else if (enemyPacketQueue.Count > 0)
            ReceivePacket(GetNextEnemyPacketFromQueue());
        else
            console.WriteConsoleMessage("Waiting for enemy");
    }

    private RelayPacket GetNextEnemyPacketFromQueue()
    {
        RelayPacket savedEnemyPacket = enemyPacketQueue[0];
        enemyPacketQueue.RemoveAt(0);
        return savedEnemyPacket;
    }

    public void ReceivePacket(RelayPacket packet) // Called by RelayCore
    {
        DebugPacket(packet);

        bool isAllyPacket = IsAllyPacket(packet);

        // If expecting 0 (if the enemy sent the packet before ally player is ready for it)
        if (expectedPackets == 0)
        {
            // Wait until packet is needed
            enemyPacketQueue.Add(packet);
            return;
        }
        else if (expectedPackets == 1) // If expecting 1
        {
            singlePacket = packet;
            // Execute packets below
        }
        else if (isAllyPacket) // If expecting 2 and packet is ally
        {
            allyPacket = packet;

            // If there's an enemy packet in queue
            if (enemyPacketQueue.Count > 0)
            {
                enemyPacket = GetNextEnemyPacketFromQueue();
                // Execute packets below
            }
            else
            {
                // Wait for enemy packet
                console.WriteConsoleMessage("Waiting for enemy");
                return;
            }
        }
        else // If expecting 2 and packet is enemy
        {
            // If ally packet exists
            if (allyPacket.actionType != null)
            {
                enemyPacket = packet;
                // Execute packets below
            }
            else // Wait for ally packet
            {
                enemyPacketQueue.Add(packet);
                return;
            }
        }

        // Reset expectedPackets before executing packet(s) in case enemy packets arrive before they're needed
        expectedPackets = 0;

        // Execute packet
        if (singlePacket.actionType == null)
            TieBreaker();
        else
            WriteActionMessage();
    }


    // Tiebreaker methods:
    private void TieBreaker()
    {
        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = SlotAssignment.Elementals[enemyPacket.casterSlot];

        int allyTimeScale = GetTimeScale(allyPacket);
        int enemyTimeScale = GetTimeScale(enemyPacket);

        // If both players passed
        if (allyPacket.actionType == "pass" && enemyPacket.actionType == "pass")
        {
            ResetPackets();

            if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            {
                // Switch back to TimeScale RoundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                console.WriteConsoleMessage("Both players have passed", null, WriteSpellEffectMessage);
            }
            else if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
                console.WriteConsoleMessage("Both players have passed. Round will end", null, RoundEnd);
            // If both players pass at RoundStart/End, continue without writing to console
            else if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
            {
                clock.NewRoundState(Clock.RoundState.TimeScale);
                NewCycle();
            }
            //.else if roundend, continue without writing to console--but what happens next?
        }

        // If both players Retreated
        else if (allyPacket.actionType == "retreat" && enemyPacket.actionType == "retreat")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { allyPacket.targetSlots[0], enemyPacket.targetSlots[0] }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will Retreat", null, CallEffectMethod);
        }

        // Check if either player Retreated. If so, isolate that player's packet and write action message
        else if (CheckForActionType("retreat"))
            return;

        // If both players activated Gems
        else if (allyPacket.actionType == "gem" && enemyPacket.actionType == "gem")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will activate their Gems", null, CallEffectMethod);
        }

        // Check if either player activated a Gem. If so, isolate that player's packet and write action message
        else if (CheckForActionType("gem"))
            return;

        // If both players sent a Spark
        else if (allyPacket.actionType == "spark" && enemyPacket.actionType == "spark")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will fling their Sparks", null, WriteActionMessage);
        }

        // Check if either player sent a Spark. If so, isolate that player's packet and write action message
        else if (CheckForActionType("spark"))
            return;

        // If both players cast a Trait
        else if (allyPacket.actionType == "trait" && enemyPacket.actionType == "trait")
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Your " + allyCaster.name + " and the enemy's " + enemyCaster.name + " will cast their Traits", null, WriteActionMessage);
        }

        // Check if either player cast a Trait. If so, isolate that player's packet and write action message
        else if (CheckForActionType("trait"))
            return;

        // If counter time
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            // If you passed, CallEffectMethod without writing a message
            if (allyPacket.actionType == "pass")
            {
                IsolatePacket(enemyPacket);
                WriteActionMessage();
            }
            else if (enemyPacket.actionType == "pass")
            {
                IsolatePacket(allyPacket);
                console.WriteConsoleMessage("The enemy has passed", null, WriteActionMessage);
            }

            // Use Elemental's MaxHealth to determine Elemental speed
            else if (allyCaster.MaxHealth < enemyCaster.MaxHealth)
            {
                targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
                IsolatePacket(allyPacket);
                console.WriteConsoleMessage("Your " + allyCaster.name + " outsped the enemy's " + enemyCaster.name, null, WriteActionMessage);
            }
            else if (allyCaster.MaxHealth > enemyCaster.MaxHealth)
            {
                targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
                IsolatePacket(enemyPacket);
                console.WriteConsoleMessage("The enemy's " + enemyCaster.name + " outsped your " + allyCaster.name, null, WriteActionMessage);
            }

            // If Elemental speeds tied
            else
            {
                targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
                console.WriteConsoleMessage("Your " + allyCaster.name + " tied with the enemy's " + enemyCaster.name, null, WriteActionMessage);
            }
        }

        // If only one player Passed
        else if (allyPacket.actionType == "pass")
        {
            IsolatePacket(enemyPacket);
            console.WriteConsoleMessage("You have passed", "The enemy will act at " + enemyTimeScale + ":00", WriteActionMessage);
        }
        else if (enemyPacket.actionType == "pass")
        {
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("You will act at " + allyTimeScale + ":00", "The enemy has passed", WriteActionMessage);
        }

        // If one player declared a sooner timescale
        else if (allyTimeScale > enemyTimeScale)
        {
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("You will act first at " + allyTimeScale + ":00", "The enemy planned to act at " + enemyTimeScale + ":00", WriteActionMessage);
        }
        else if (enemyTimeScale > allyTimeScale)
        {
            IsolatePacket(enemyPacket);
            console.WriteConsoleMessage("You planned to act at " + allyTimeScale + ":00", "The enemy will act first at " + enemyTimeScale + ":00", WriteActionMessage);
        }

        // If timescales tied, use Elemental's MaxHealth to determine Elemental speed
        else if (allyCaster.MaxHealth < enemyCaster.MaxHealth)
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                "Your " + allyCaster.name + " outsped the enemy's " + enemyCaster.name, null, WriteActionMessage);
        }
        else if (allyCaster.MaxHealth > enemyCaster.MaxHealth)
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            IsolatePacket(enemyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                "The enemy's " + enemyCaster.name + " outsped your " + allyCaster.name, null, WriteActionMessage);
        }

        // If timescales and Elemental speeds tied
        else
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimeScale + ":00. " +
                    "Your " + allyCaster.name + " tied with the enemy's " + enemyCaster.name, null, WriteActionMessage);
        }
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
    private bool CheckForActionType(string actionType)
    {
        // Check if either single packet is of actionType. If so, write the action message for it
        // If both packets are of actionType, this method won't be called

        if (allyPacket.actionType == actionType)
        {
            IsolatePacket(allyPacket);
            WriteActionMessage();
            return true;
        }
        else if (enemyPacket.actionType == actionType)
        {
            IsolatePacket(enemyPacket);
            WriteActionMessage();
            return true;
        }

        return false;
    }
    private void IsolatePacket(RelayPacket newSinglePacket)
    {
        ResetPackets();
        singlePacket = newSinglePacket;
    }


    // ActionMessage methods:
    public void WriteActionMessage()
    {
        // If there's one packet to write, write it and prepare the correct output method
        if (singlePacket.actionType != null)
        {
            // If the acting player passed
            //.at the moment, this is only possible during counter time. In the future, I might need to update this code or make a new method for this situation
            //.I think this is messy anyway. It works, but it'd be nice to tidy this path up a bit
            if (singlePacket.actionType == "pass")
            {
                // Switch back to TimeScale RoundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                // If counter time and you passed, WriteSpellEffectMessage without writing that you've passed
                if (IsAllyPacket(singlePacket))
                    WriteSpellEffectMessage();
                else
                    console.WriteConsoleMessage("The enemy has passed", null, WriteSpellEffectMessage);

                return;
            }

            (string, Console.OutputMethod) messageAndOutput = GenerateActionMessage(singlePacket);
            console.WriteConsoleMessage(messageAndOutput.Item1, null, messageAndOutput.Item2);
        }
        // If there's two packets to write, write ally message and prepare to write enemy message
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

        string packetOwner = IsAllyPacket(packet) ? "Your" : "The enemy's";
        string caster = packetOwner + " " + casterElemental.name;

        if (packet.actionType == "retreat")
            return (caster + " will Retreat", CallEffectMethod);

        if (packet.actionType == "gem")
            return (caster + " will activate its Gem", CallEffectMethod);

        if (packet.actionType == "spark")
            return (caster + " will fling its Spark at the target", CallEffectMethod);

        if (packet.actionType == "trait")
            return (caster + " will cast " + casterElemental.trait.Name, CallEffectMethod);

        // Below code only reached if packet is a Spell:

        if (packet.name == "Hex") // *Hex
        {
            string hexEffect = "Slowing";
            if (packet.hexType == "poison")
                hexEffect = "Poisoning";
            else if (packet.hexType == "weaken")
                hexEffect = "Weakening";

            return (caster + " will cast Hex, " + hexEffect + " the target", CheckForCounter);
        }

        // If counter, counter spell will occur before checking for counter again
        Console.OutputMethod spellOutputMethod = Clock.CurrentRoundState == Clock.RoundState.TimeScale ? CheckForCounter : CallEffectMethod;

        if (packet.frenzy) // *Frenzy
        {
            if (packet.potion)
                return (caster + " will activate Frenzy and drink its Potion, then cast " + packet.name, spellOutputMethod);

            return (caster + " will activate Frenzy, then cast " + packet.name, spellOutputMethod);
        }

        if (packet.potion)
            return (caster + " will drink its Potion, then cast " + packet.name, spellOutputMethod);

        return (caster + " will cast " + packet.name, spellOutputMethod);
    }


    // CheckForCounter methods:
    public void CheckForCounter()
    {
        targetManager.ResetAllTargets();

        // If it isn't already counter time, save packets
        if (Clock.CurrentRoundState != Clock.RoundState.Counter)
        {
            // Save packets before requesting new ones
            SavePackets();

            // Switch to counter roundState before calling CheckForAvailableActions
            clock.NewRoundState(Clock.RoundState.Counter);
        }
        // If a counter effect has already occurred, reset packets before requesting new ones
        else
            ResetPackets();

        bool isHost = NetworkManager.Singleton.IsHost;
        int allyPlayerNumber = isHost ? 0 : 1;
        int enemyPlayerNumber = isHost ? 1 : 0;

        // If single counter
        if (savedSinglePacket.Item1.actionType != null)
        {
            bool isCounteringPlayer = !IsAllyPacket(savedSinglePacket.Item1);

            // If countering player has no available actions, write to console and prepare CallEffectMethod
            int counteringPlayerNumber = isCounteringPlayer ? allyPlayerNumber : enemyPlayerNumber;

            if (!CheckForAvailableActions(isCounteringPlayer))
            {
                // Switch back to TimeScale RoundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                string counteringPlayer = isCounteringPlayer ? "You have" : "The enemy has";
                console.WriteConsoleMessage(counteringPlayer + " no available counter actions", null, WriteSpellEffectMessage);
            }
            else // If counter action is available, request counter packet
                RequestPacket(isCounteringPlayer, !isCounteringPlayer);
        }
        else // If multiple counter
        {
            bool allyCounterAvailable = CheckForAvailableActions(true);
            bool enemyCounterAvailable = CheckForAvailableActions(false);

            if (!allyCounterAvailable && !enemyCounterAvailable)
            {
                // Switch back to TimeScale RoundState
                clock.NewRoundState(Clock.RoundState.TimeScale);

                console.WriteConsoleMessage("Neither player has available counter actions", null, WriteSpellEffectMessage);
            }
            else if (allyCounterAvailable && enemyCounterAvailable)
                RequestPacket(true, true);
            else if (allyCounterAvailable && !enemyCounterAvailable)
                // Inform the player that the enemy cannot counter before requesting ally packet
                console.WriteConsoleMessage("The enemy has no available counter actions", null, EnemyCannotCounter);
            else // If only enemy counter available
                 // Inform the player that they cannot counter before requesting enemy packet
                console.WriteConsoleMessage("You have no available counter actions", null, AllyCannotCounter);
        }
    }
    private void SavePackets()
    {
        // Caster Elementals are cached in case casters Swap before their Spell occurs
        // No need to store messages as SparkInfos do, since casters can't be Eliminated at counter speed

        Elemental singleCaster = SlotAssignment.Elementals[singlePacket.casterSlot];
        savedSinglePacket = (singlePacket, singleCaster);

        Elemental allyCaster = SlotAssignment.Elementals[allyPacket.casterSlot];
        savedAllyPacket = (allyPacket, allyCaster);

        Elemental enemyCaster = SlotAssignment.Elementals[enemyPacket.casterSlot];
        savedEnemyPacket = (enemyPacket, enemyCaster);

        ResetPackets();
    }
    public void EnemyCannotCounter()
    {
        RequestPacket(true, false);
    }
    public void AllyCannotCounter()
    {
        RequestPacket(false, true);
    }


    // WriteSpellEffect methods:
        // These methods restore saved packets and write TimeScale SpellEffect messages before CallEffectMethod occurs
        // These methods are not called for counter SpellEffects
    public void WriteSpellEffectMessage()
    {
        if (savedSinglePacket.Item1.actionType != null)
        {
            DisplaySavedSpellTargets(savedSinglePacket);

            string message = GenerateSpellEffectMessage(savedSinglePacket);
            RestoreSavedPackets();
            console.WriteConsoleMessage(message, null, CallEffectMethod);
        }
        else
        {
            DisplaySavedSpellTargets(savedAllyPacket);

            string allyMessage = GenerateSpellEffectMessage(savedAllyPacket);
            console.WriteConsoleMessage(allyMessage, null, WriteEnemySpellEffectMessage);
        }
    }
    public void WriteEnemySpellEffectMessage()
    {
        DisplaySavedSpellTargets(savedEnemyPacket);

        string enemyMessage = GenerateSpellEffectMessage(savedEnemyPacket);
        RestoreSavedPackets();
        console.WriteConsoleMessage(enemyMessage + " simultaneously", null, CallEffectMethod);
    }
    private string GenerateSpellEffectMessage((RelayPacket, Elemental) savedSpellPacket)
    {
        string casterOwner = savedSpellPacket.Item2.isAlly ? "Your " : "The enemy ";
        return casterOwner + savedSpellPacket.Item2.name + "'s " + savedSpellPacket.Item1.name + " will occur";
    }
    private void DisplaySavedSpellTargets((RelayPacket, Elemental) savedSpellPacket)
    {
        int casterSlot = SlotAssignment.GetSlot(savedSpellPacket.Item2);
        targetManager.DisplayTargets(new() { casterSlot }, savedSpellPacket.Item1.targetSlots.ToList(), false);
    }
    private void RestoreSavedPackets()
    {
        singlePacket = savedSinglePacket.Item1;
        allyPacket = savedAllyPacket.Item1;
        enemyPacket = savedEnemyPacket.Item1;
        ResetSavedPackets();
    }

    // Normal EffectMethods:
    public void CallEffectMethod()
    {
        targetManager.ResetAllTargets();

        // Determine which effect method is correct
        RelayPacket switchPacket = singlePacket.actionType != null ? singlePacket : allyPacket;

        EffectDelegate effectDelegate = switchPacket.actionType switch
        {
            "retreat" => RetreatEffect,
            "gem" => GemEffect,
            "spark" => SparkEffect,
            "trait" => TraitEffect,
            _ => SpellEffect,
        };

        // Call the effect method(s)
        if (singlePacket.actionType != null)
            effectDelegate(singlePacket);
        else
        {
            effectDelegate(allyPacket);
            effectDelegate(enemyPacket);
        }

        if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            CheckForCounter();
        else if (switchPacket.actionType == "spell" && flungSparks.Count > 0)
            WriteSparkDamageMessage();
        else
            NewCycle();
    }

    private void RetreatEffect(RelayPacket packet)
    {
        // Remove action/Add armor before swapping
        SlotAssignment.Elementals[packet.casterSlot].currentActions -= 1;
        SlotAssignment.Elementals[packet.targetSlots[0]].ToggleArmored(true);

        SlotAssignment.Swap(packet.casterSlot, packet.targetSlots[0]);

        //.add lateeffect to remove armor at end of round
    }

    private void GemEffect(RelayPacket packet)
    {
        Elemental caster = SlotAssignment.Elementals[packet.casterSlot];
        caster.HealthChange(2);
        caster.ToggleGem(false);
    }

    private void SparkEffect(RelayPacket packet)
    {
        Elemental sparkCaster = SlotAssignment.Elementals[packet.casterSlot];

        // Cache message in sparkInfo list in case caster is null (Eliminated) when the message needs to be written
        string casterOwner = sparkCaster.isAlly ? "Your" : "The enemy's";
        string sparkMessage = casterOwner + " " + sparkCaster.name + "'s Spark will deal 1 to the target";

        SparkInfo sparkInfo = new()
        {
            caster = sparkCaster,
            targetSlot = packet.targetSlots[0],
            message = sparkMessage
        };
        flungSparks.Add(sparkInfo);

        sparkCaster.ToggleSpark(false);
    }

    private void TraitEffect(RelayPacket packet)
    {
        Debug.Log("TraitEffect");
    }

    private void SpellEffect(RelayPacket packet)
    {
        Debug.Log("SpellEffect");
    }

    private void WriteSparkDamageMessage()
    {
        // Check if the target is still available
        Elemental sparkTarget = SlotAssignment.Elementals[flungSparks[0].targetSlot];
        if (sparkTarget == null || sparkTarget.DisengageStrength > 0)
        {
            CycleSparks();
            return;
        }

        // Don't dim caster if it's still alive
        List<int> casterList = new();
        if (flungSparks[0].caster != null)
        {
            int casterSlot = SlotAssignment.GetSlot(flungSparks[0].caster);
            casterList.Add(casterSlot);
        }
        targetManager.DisplayTargets(casterList, new List<int>() { flungSparks[0].targetSlot }, false);

        console.WriteConsoleMessage(flungSparks[0].message, null, SparkDamage);
    }
    public void SparkDamage()
    {
        targetManager.ResetAllTargets();

        Elemental target = SlotAssignment.Elementals[flungSparks[0].targetSlot];
        target.TakeDamage(1, flungSparks[0].caster);

        CycleSparks();
    }
    private void CycleSparks()
    {
        flungSparks.RemoveAt(0);

        if (flungSparks.Count > 0)
            WriteSparkDamageMessage();
        else
            NewCycle();
    }


    //.not sure what to call these methods yet:
    private void RoundEnd()
    {
        //make clock say 0:00

        Debug.Log("RoundEnd");
    }

    public void RoundStart() // Called by Setup
    {
        //.make clock say 7:00

        // Reset actions and Armored
        for (int i = 0; i < 8; i++)
        {
            Elemental elemental = SlotAssignment.Elementals[i];
            if (elemental != null)
                elemental.currentActions = i < 4 ? 1 : 0;
        }

        //.delayed effects occur simultaneously and silently
        //.cycle text messages using preset order (see bible)

        NewCycle();
    }

    private void NewCycle()
    {
        ResetPackets();

        //.if immediate available, do immediate things
        //.if no actions available, autopass and "You have no available actions"
        //.if only one player has an action available, do it similarly to counter stuff. No autopassing!!

        // Request roundstart/end/timescale packets
        RequestPacket(true, true);
    }



    //.not sure what to call these methods yet:
    private bool CheckForAvailableActions(bool isAlly)
    {
        // Check all allied Elementals in play for available actions
        for (int i = 0; i < 4; i++)
        {
            Elemental elemental = SlotAssignment.Elementals[i];
            if ((elemental.isAlly == isAlly) && elemental.CanAct())
                return true;
        }

        return false;
    }

    //private bool CheckForGameOver()
    //{
    //    //.eliminate elementals below 0 health

    //    return false;
    //}

    private bool IsAllyPacket(RelayPacket packet)
    {
        return packet.player == 0 == NetworkManager.Singleton.IsHost;
    }

    private void ResetPackets()
    {
        singlePacket = default;
        allyPacket = default;
        enemyPacket = default;
    }
    private void ResetSavedPackets()
    {
        savedSinglePacket = default;
        savedAllyPacket = default;
        savedEnemyPacket = default;
    }
}
public struct SparkInfo
{
    public Elemental caster;
    public int targetSlot;
    public string message;
}