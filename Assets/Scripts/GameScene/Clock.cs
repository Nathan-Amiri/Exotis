using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    //no roundstartmessages currently exist in game
    public enum RoundState { RoundStartDelay, RoundStartCycle, TimeScale, RoundEndCycle, RoundEndMessage, RoundEndDelay, Repopulate}
    public static RoundState CurrentRoundState {  get; private set; }

    public static int CurrentTimeScale;

    private void Start()
    {
        //start with RoundStartCycles since there can be no DelayedEffects at the start of a game
        CurrentRoundState = RoundState.RoundStartCycle;

        CurrentTimeScale = 7;
    }

    //only called by ExecutionCore
    public void NewRoundState(RoundState newState)
    {
        CurrentRoundState = newState;
    }

    //only called by ExecutionCore
    public void NewTimeScale(int newScale)
    {
        CurrentTimeScale = newScale;
    }
}