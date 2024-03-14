using System;
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
    [SerializeField] private SlotAssignment slotAssignment;
    [SerializeField] private Clock clock;
    [SerializeField] private Console console;
    [SerializeField] private TargetManager targetManager;
    [SerializeField] private SpellTraitEffect spellTraitEffect;

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

    private readonly List<SpellTraitEffectInfo> roundStartInfos = new();
    private List<SpellTraitEffectInfo> roundEndInfos = new();
    private readonly List<SpellTraitEffectInfo> nextRoundEndInfos = new();

    // Round/Cycle methods:
    public void RoundStart() // Called by Setup
    {
        // Update clock
        clock.NewTimescale(7);
        clock.NewRoundState(Clock.RoundState.RoundStart);

        // Reset actions and Armored
        for (int i = 0; i < 8; i++)
        {
            Elemental elemental = slotAssignment.Elementals[i];
            if (elemental != null)
                elemental.currentActions = i < 4 ? 1 : 0;
        }

        // RoundStart Delayed Effects
        foreach (SpellTraitEffectInfo info in roundStartInfos)
            spellTraitEffect.CallEffectMethod(info);

        roundStartInfos.Clear();

        // Start new cycle
        NewCycle();
    }

    private void NewCycle()
    {
        ResetPackets();

        // Set clock to 0:00 if it's the end of the round
        if (Clock.CurrentRoundState == Clock.RoundState.RoundEnd)
            clock.NewTimescale(0);

        bool allyActionAvailable = CheckForAvailableActions(true);
        bool enemyActionAvailable = CheckForAvailableActions(false);

        // If neither player can act
        if (!allyActionAvailable && !enemyActionAvailable)
        {
            // If neither player can act at RoundStart, do not write
            if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
            {
                clock.NewRoundState(Clock.RoundState.Timescale);
                NewCycle();
            }
            // If neither player can act at timescale, write and prepare to request RoundEnd delegations
            else if (Clock.CurrentRoundState == Clock.RoundState.Timescale)
            {
                clock.NewRoundState(Clock.RoundState.Timescale);
                console.WriteConsoleMessage("Neither player has any available actions. Round will end", null, NewCycle);
            }
            // If neither player can act at RoundEnd, do not write
            else
                RoundEnd();
        }
        else
            RequestPacket(allyActionAvailable, enemyActionAvailable);
    }

    public void RoundEnd()
    {
        //.blubber first
        //.high voltage second, right before poison

        // Deal Poison damage to all Elementals in play
        for (int i = 0; i < 4; i++)
        {
            Elemental elemental = slotAssignment.Elementals[i];
            if (elemental.PoisonStrength > 0)
                elemental.DealDamage(1, elemental, false);
        }

        if (CheckForGameEnd())
            return;

        // RoundEnd Delayed Effects
        foreach (SpellTraitEffectInfo info in roundEndInfos)
            spellTraitEffect.CallEffectMethod(info);

        roundEndInfos = nextRoundEndInfos;
        nextRoundEndInfos.Clear();

        Repopulation();
    }

    private void Repopulation()
    {
        bool isHost = NetworkManager.Singleton.IsHost;

        // Check if ally/enemy can repopulate
        bool allyCanRepopulate = CheckIfRepopulationDelegationNeeded(isHost);
        bool enemyCanRepopulate = CheckIfRepopulationDelegationNeeded(!isHost);

        if (!allyCanRepopulate && !enemyCanRepopulate)
            RoundStart();
        else
        {
            clock.NewRoundState(Clock.RoundState.Repopulation);
            RequestPacket(allyCanRepopulate, enemyCanRepopulate);
        }
    }
    private bool CheckIfRepopulationDelegationNeeded(bool checkHostPlayer)
    {
        // If checking guest player's slots, add 2
        int a = checkHostPlayer ? 0 : 2;

        List<bool> slotIsEmpty = new()
        {
            slotAssignment.Elementals[0 + a] == null,
            slotAssignment.Elementals[1 + a] == null,
            slotAssignment.Elementals[4 + a] == null,
            slotAssignment.Elementals[5 + a] == null
        };

        // Return if both in play slots are full or if both benched slots are empty
        if (!slotIsEmpty[0] && !slotIsEmpty[1])
            return false;

        if (slotIsEmpty[4] && slotIsEmpty[5])
            return false;

        // If both in play slots are empty, repopulate automatically
        if (slotIsEmpty[0] && slotIsEmpty[1])
        {
            if (!slotIsEmpty[4])
            {
                slotAssignment.Repopulate(0 + a, 4 + a);
                if (!slotIsEmpty[5])
                    slotAssignment.Repopulate(1 + a, 5 + a);
            }
            else
                slotAssignment.Repopulate(0 + a, 5 + a);

            return false;
        }

        // If either benched slot is missing, repopulate automatically
        if (slotIsEmpty[4] || slotIsEmpty[5])
        {
            int inPlaySlot = slotIsEmpty[0] ? 0 + a : 1 + a;
            int benchedSlot = !slotIsEmpty[4] ? 4 + a : 5 + a;
            slotAssignment.Repopulate(inPlaySlot, benchedSlot);

            return false;
        }
        
        return true;
    }
    private void ReceiveRepopulationPacket()
    {
        if (singlePacket.actionType != null)
            RepopulateFromPacket(singlePacket);
        else
        {
            RepopulateFromPacket(allyPacket);
            RepopulateFromPacket(enemyPacket);
        }

        RoundStart();
    }
    private void RepopulateFromPacket(RelayPacket packet)
    {
        // If checking guest player's slots, add 2
        int a = packet.player == 0 ? 0 : 2;
        int emptyInPlaySlot = slotAssignment.Elementals[0 + a] == null ? 0 + a : 1 + a;

        slotAssignment.Repopulate(emptyInPlaySlot, packet.targetSlots[0]);
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

            if (enemyPacketQueue.Count > 0)
            {
                enemyPacket = GetNextEnemyPacketFromQueue();
                // Execute packets below
            }
            else
            {
                console.WriteConsoleMessage("Waiting for enemy");
                return;
            }
        }
        else // If expecting 2 and packet is enemy
        {
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

        if (Clock.CurrentRoundState == Clock.RoundState.Repopulation)
        {
            ReceiveRepopulationPacket();
            return;
        }

        // Execute packet
        if (singlePacket.actionType == null)
            TieBreaker();
        else
            WriteActionMessage();
    }

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

        if (packet.wildTimescale != 0)
            debugMessage += ", wild[" + packet.wildTimescale + "]";

        if (packet.hexType != string.Empty) // *Hex
            debugMessage += "[" + packet.hexType + "]";

        if (packet.potion)
            debugMessage += ", potion";

        if (packet.frenzy)
            debugMessage += ", frenzy";

        Debug.Log(debugMessage);
    }

    // Tiebreaker methods:
    private void TieBreaker()
    {
        Elemental allyCaster = slotAssignment.Elementals[allyPacket.casterSlot];
        Elemental enemyCaster = slotAssignment.Elementals[enemyPacket.casterSlot];

        int allyTimescale = GetTimescale(allyPacket);
        int enemyTimescale = GetTimescale(enemyPacket);

        // If both players passed
        if (allyPacket.actionType == "pass" && enemyPacket.actionType == "pass")
            DoublePass();

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

            // If one caster outsped the other
            else if (allyCaster.Speed > enemyCaster.Speed)
            {
                targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
                IsolatePacket(allyPacket);
                console.WriteConsoleMessage("Your " + allyCaster.name + " outsped the enemy's " + enemyCaster.name, null, WriteActionMessage);
            }
            else if (allyCaster.Speed < enemyCaster.Speed)
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
            console.WriteConsoleMessage("You have passed", "The enemy will act at " + enemyTimescale + ":00", WriteActionMessage);
        }
        else if (enemyPacket.actionType == "pass")
        {
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("You will act at " + allyTimescale + ":00", "The enemy has passed", WriteActionMessage);
        }

        // If one player declared a sooner timescale
        else if (allyTimescale > enemyTimescale)
        {
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("You will act first at " + allyTimescale + ":00", "The enemy planned to act at " + enemyTimescale + ":00", WriteActionMessage);
        }
        else if (enemyTimescale > allyTimescale)
        {
            IsolatePacket(enemyPacket);
            console.WriteConsoleMessage("You planned to act at " + allyTimescale + ":00", "The enemy will act first at " + enemyTimescale + ":00", WriteActionMessage);
        }

        // If timescales tied, compare caster speed
        else if (allyCaster.Speed > enemyCaster.Speed)
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            IsolatePacket(allyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimescale + ":00. " +
                "Your " + allyCaster.name + " outsped the enemy's " + enemyCaster.name, null, WriteActionMessage);
        }
        else if (allyCaster.Speed < enemyCaster.Speed)
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            IsolatePacket(enemyPacket);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimescale + ":00. " +
                "The enemy's " + enemyCaster.name + " outsped your " + allyCaster.name, null, WriteActionMessage);
        }

        // If timescales and caster speeds tied
        else
        {
            targetManager.DisplayTargets(new List<int> { allyPacket.casterSlot, enemyPacket.casterSlot }, new List<int> { }, false);
            console.WriteConsoleMessage("Both players planned to act at " + allyTimescale + ":00. " +
                    "Your " + allyCaster.name + " tied with the enemy's " + enemyCaster.name, null, WriteActionMessage);
        }
    }
    private int GetTimescale(RelayPacket packet)
    {
        if (packet.actionType != "spell")
            return 0;

        if (packet.wildTimescale != 0)
            return packet.wildTimescale;

        List<Spell> casterSpells = slotAssignment.Elementals[packet.casterSlot].spells;
        Spell castSpell = null;
        foreach (Spell spell in casterSpells)
            if (spell.Name == packet.name)
            {
                castSpell = spell;
                break;
            }
        if (castSpell == null)
            Debug.LogError("Caster's Spell not found");

        return castSpell.Timescale;
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
            if (singlePacket.actionType == "pass")
            {
                SinglePass();
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

        Elemental casterElemental = slotAssignment.Elementals[packet.casterSlot];

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

            return (caster + " will cast Hex, " + hexEffect + " the target", NewCounterCycle);
        }

        // If counter, counter spell will occur before checking for counter again
        Console.OutputMethod spellOutputMethod = Clock.CurrentRoundState == Clock.RoundState.Timescale ? NewCounterCycle : CallEffectMethod;

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

    // Pass methods:
    private void DoublePass()
    {
        ResetPackets();

        if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
        {
            clock.NewRoundState(Clock.RoundState.Timescale);
            NewCycle();
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.Timescale)
        {
            clock.NewRoundState(Clock.RoundState.RoundEnd);
            console.WriteConsoleMessage("Both players have passed. Round will end", null, NewCycle);
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            clock.NewRoundState(Clock.RoundState.Timescale);
            console.WriteConsoleMessage("Both players have passed", null, WriteSpellEffectMessage);
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.RoundEnd)
            RoundEnd();
    }
    private void SinglePass()
    {
        ResetPackets();

        if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
        {
            clock.NewRoundState(Clock.RoundState.Timescale);
            NewCycle();
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.Timescale)
        {
            clock.NewRoundState(Clock.RoundState.RoundEnd);

            // If you passed, don't write message
            if (IsAllyPacket(singlePacket))
                NewCycle();
            else
                console.WriteConsoleMessage("The enemy has passed. Round will end", null, NewCycle);
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
        {
            clock.NewRoundState(Clock.RoundState.Timescale);

            // If you passed, don't write message
            if (!IsAllyPacket(singlePacket))
                WriteSpellEffectMessage();
            else
                console.WriteConsoleMessage("The enemy has passed", null, WriteSpellEffectMessage);
        }
        else if (Clock.CurrentRoundState == Clock.RoundState.RoundEnd)
            RoundEnd();
    }

    // NewCounterCycle methods:
    public void NewCounterCycle()
    {
        // This method performs a similar role to NewCycle, but only handles counter cycles (which occur inside normal cycles)

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

            if (!CheckForAvailableActions(isCounteringPlayer))
            {
                clock.NewRoundState(Clock.RoundState.Timescale);

                string counteringPlayer = isCounteringPlayer ? "You have" : "The enemy has";
                console.WriteConsoleMessage(counteringPlayer + " no available counter actions", null, WriteSpellEffectMessage);
            }
            else
                RequestPacket(isCounteringPlayer, !isCounteringPlayer);
        }
        else // If multiple counter
        {
            bool allyCounterAvailable = CheckForAvailableActions(true);
            bool enemyCounterAvailable = CheckForAvailableActions(false);

            if (!allyCounterAvailable && !enemyCounterAvailable)
            {
                clock.NewRoundState(Clock.RoundState.Timescale);

                console.WriteConsoleMessage("Neither player has any available counter actions", null, WriteSpellEffectMessage);
            }
            else
                RequestPacket(allyCounterAvailable, enemyCounterAvailable);
        }
    }
    private void SavePackets()
    {
        // Caster Elementals are cached in case casters Swap before their Spell occurs
        // No need to store messages as SparkInfos do, since casters can't be Eliminated at counter speed

        Elemental singleCaster = slotAssignment.Elementals[singlePacket.casterSlot];
        savedSinglePacket = (singlePacket, singleCaster);

        Elemental allyCaster = slotAssignment.Elementals[allyPacket.casterSlot];
        savedAllyPacket = (allyPacket, allyCaster);

        Elemental enemyCaster = slotAssignment.Elementals[enemyPacket.casterSlot];
        savedEnemyPacket = (enemyPacket, enemyCaster);

        ResetPackets();
    }

    // WriteSpellEffect methods:
        // These methods restore saved packets and write Timescale SpellEffect messages before CallEffectMethod occurs
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
        int casterSlot = slotAssignment.GetSlot(savedSpellPacket.Item2);
        targetManager.DisplayTargets(new() { casterSlot }, savedSpellPacket.Item1.targetSlots.ToList(), false);
    }
    private void RestoreSavedPackets()
    {
        singlePacket = savedSinglePacket.Item1;
        allyPacket = savedAllyPacket.Item1;
        enemyPacket = savedEnemyPacket.Item1;
        ResetSavedPackets();
    }

    // Normal Effect Methods:
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

        if (CheckForGameEnd())
            return;

        if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            NewCounterCycle();
        else if (switchPacket.actionType == "spell" && flungSparks.Count > 0)
            WriteSparkDamageMessage();
        else
            NewCycle();
    }

    private void RetreatEffect(RelayPacket packet)
    {
        // Remove action/Add armor before swapping
        slotAssignment.Elementals[packet.casterSlot].currentActions -= 1;
        slotAssignment.Elementals[packet.targetSlots[0]].ToggleArmored(true);

        slotAssignment.Swap(packet.casterSlot, packet.targetSlots[0]);

        //.add lateeffect to remove armor at end of round
    }

    private void GemEffect(RelayPacket packet)
    {
        Elemental caster = slotAssignment.Elementals[packet.casterSlot];
        caster.HealthChange(2);
        caster.ToggleGem(false);
    }

    private void SparkEffect(RelayPacket packet)
    {
        Elemental sparkCaster = slotAssignment.Elementals[packet.casterSlot];

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
        SpellTraitEffectInfo info = ConvertPacketToSpellTraitEffectInfo(packet);

        spellTraitEffect.CallEffectMethod(info);
    }

    private void SpellEffect(RelayPacket packet)
    {
        int newTimeScale = GetTimescale(packet);
        clock.NewTimescale(newTimeScale);

        SpellTraitEffectInfo info = ConvertPacketToSpellTraitEffectInfo(packet);

        // Turn on Potion/Frenzy boosting before SpellTraitEffect class calls TakeDamage
        info.caster.potionBoosting = packet.potion;
        info.caster.frenzyBoosting = packet.frenzy;

        info.caster.currentActions -= 1;
        spellTraitEffect.CallEffectMethod(info);

        // Immediately turn Potion/Frenzy boosting off again
        info.caster.potionBoosting = false;
        info.caster.frenzyBoosting = false;
    }

    private SpellTraitEffectInfo ConvertPacketToSpellTraitEffectInfo(RelayPacket packet)
    {
        List<Elemental> newTargets = new();
        foreach (int targetSlot in packet.targetSlots)
            newTargets.Add(slotAssignment.Elementals[targetSlot]);

        return new SpellTraitEffectInfo()
        {
            occurance = 0,
            spellOrTraitName = packet.name,
            caster = slotAssignment.Elementals[packet.casterSlot],
            targets = newTargets,
            hexType = packet.hexType
        };
    }

    // Spark Damage Methods:
    private void WriteSparkDamageMessage()
    {
        // Check if the target is still available
        Elemental sparkTarget = slotAssignment.Elementals[flungSparks[0].targetSlot];
        if (sparkTarget == null || sparkTarget.DisengageStrength > 0)
        {
            CycleSparks();
            return;
        }

        // Don't dim caster if it's still alive
        List<int> casterList = new();
        if (flungSparks[0].caster != null)
        {
            int casterSlot = slotAssignment.GetSlot(flungSparks[0].caster);
            casterList.Add(casterSlot);
        }
        targetManager.DisplayTargets(casterList, new List<int>() { flungSparks[0].targetSlot }, false);

        console.WriteConsoleMessage(flungSparks[0].message, null, SparkDamage);
    }
    public void SparkDamage()
    {
        targetManager.ResetAllTargets();

        Elemental target = slotAssignment.Elementals[flungSparks[0].targetSlot];
        target.DealDamage(1, flungSparks[0].caster, false);

        CycleSparks();
    }
    private void CycleSparks()
    {
        flungSparks.RemoveAt(0);

        if (flungSparks.Count > 0)
        {
            WriteSparkDamageMessage();
            return;
        }

        if (CheckForGameEnd())
            return;

        NewCycle();
    }

    // Misc methods:
    private bool CheckForAvailableActions(bool isAlly)
    {
        // Check all allied Elementals in play for available actions
        for (int i = 0; i < 4; i++)
        {
            Elemental elemental = slotAssignment.Elementals[i];
            if ((elemental.isAlly == isAlly) && elemental.CanAct())
                return true;
        }

        return false;
    }

    private bool CheckForGameEnd()
    {
        // Eliminate Elementals at 0 health
        foreach (Elemental elemental in slotAssignment.Elementals)
            if (elemental.Health == 0)
                Destroy(elemental);

        // Check if game has ended
        List<int> allySlots = new() { 0, 1, 4, 5 };

        bool allAllyElementalsEliminated = true;
        bool allEnemyElementalsEliminated = true;

        for (int i = 0; i < slotAssignment.Elementals.Count; i++)
        {
            // If an ally Elemental is not Eliminated, enemy has not won, and vice versa
            if (slotAssignment.Elementals[i] == null)
                continue;

            if (allySlots.Contains(i))
                allAllyElementalsEliminated = false;
            else
                allEnemyElementalsEliminated = false;
        }

        if (!allAllyElementalsEliminated && !allEnemyElementalsEliminated)
            return false;

        if (allAllyElementalsEliminated && allEnemyElementalsEliminated)
            console.WriteConsoleMessage("All Elementals have been Eliminated simultaneously. The game ends in a tie!");
        else if (allAllyElementalsEliminated)
            console.WriteConsoleMessage("All your Elementals have been Eliminated. The enemy wins the game!");
        else // If all enemy elementals are eliminated
            console.WriteConsoleMessage("All the enemy�s Elementals have been Eliminated. You win the game!");

        return true;
    }

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

    public void AddRoundStartDelayedEffect(int newOccurance, SpellTraitEffectInfo info)
    {
        info.occurance = newOccurance;
        roundStartInfos.Add(info);
    }
    public void AddRoundEndDelayedEffect(int newOccurance, SpellTraitEffectInfo info)
    {
        info.occurance = newOccurance;
        roundEndInfos.Add(info);
    }
    public void AddNextRoundEndDelayedEffect(int newOccurance, SpellTraitEffectInfo info)
    {
        info.occurance = newOccurance;
        nextRoundEndInfos.Add(info);
    }
}
public struct SparkInfo
{
    public Elemental caster;
    public int targetSlot;
    public string message;
}