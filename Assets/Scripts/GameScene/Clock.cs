using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    // STATIC:
    public enum RoundState { RoundStart, RoundEnd, Timescale, Counter, Immediate, Repopulation }
    public static RoundState CurrentRoundState { get; private set; }

    public static int CurrentTimescale { get; private set; }

    // SCENE REFERENCE:
    [SerializeField] private TMP_Text timescaleText;

    //Only called by ExecutionCore
    public void NewRoundState(RoundState newState)
    {
        CurrentRoundState = newState;
    }

    // Only called by ExecutionCore
    public void NewTimescale(int newTimescale)
    {
        CurrentTimescale = newTimescale;

        timescaleText.text = newTimescale + ":00";
    }
}