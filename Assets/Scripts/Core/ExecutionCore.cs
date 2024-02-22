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

    // DYNAMIC:
    private bool expectingSinglePacket;

    private RelayPacket singlePacket;
    private RelayPacket allyPacket;
    private RelayPacket enemyPacket;

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
        DelegationCore.DelegationScenario scenario;

        if (Clock.CurrentRoundState == Clock.RoundState.RoundStart)
            scenario = DelegationCore.DelegationScenario.RoundStart;
        else if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
            scenario = DelegationCore.DelegationScenario.TimeScale;
        else
            scenario = DelegationCore.DelegationScenario.RoundEnd;

        expectingSinglePacket = false;
        delegationCore.RequestDelegation(scenario);
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
        if (Clock.CurrentRoundState == Clock.RoundState.Repopulate)
            Repopulate();
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

    private void TieBreaker()
    {
        Debug.Log(allyPacket.player);
        Debug.Log(enemyPacket.player);
    }

    private void SingleCounter()
    {

    }

    private void CounterTieBreaker()
    {

    }

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

    private void Repopulate()
    {

    }


    private bool CheckForGameOver()
    {
        //.eliminate elementals below 0 health

        return false;
    }



    public void SelectConsoleButton()
    {

    }
}