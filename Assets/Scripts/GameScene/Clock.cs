using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    // No roundstartmessages currently exist in game
    public enum RoundState { RoundStart, TimeScale, Counter, RoundEnd, Repopulate}
    public static RoundState CurrentRoundState {  get; private set; }

    public static int CurrentTimeScale;

    private void Start()
    {
        CurrentRoundState = RoundState.RoundStart;

        CurrentTimeScale = 7;
    }

    // Only called by ExecutionCore
    public void NewRoundState(RoundState newState)
    {
        CurrentRoundState = newState;
    }

    // Only called by ExecutionCore
    public void NewTimeScale(int newScale)
    {
        CurrentTimeScale = newScale;
    }
}