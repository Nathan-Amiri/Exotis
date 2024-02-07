using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Trait : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentElemental;

    [SerializeField] private Button button;
        // Accessed by Elemental
    public TMP_Text nameText;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
        // ElementalInfo fields: (set up by Elemental)
    [NonSerialized] public bool usableRoundStart;
    [NonSerialized] public bool usableRoundEnd;
    [NonSerialized] public bool usableCounterSpeed;
    [NonSerialized] public bool usableDuringTimeScaleSpeeds;

        // IDelegationAction fields:
    public bool IsTargeted { get; private set; }

    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    // Called by Elemental
    public void SetIsTargeted(bool newIsTargeted)
    {
        IsTargeted = newIsTargeted;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!parentElemental.isAlly)
            return;

        switch (delegationScenario)
        {
            case DelegationCore.DelegationScenario.Reset:
                button.interactable = false;
                break;
            case DelegationCore.DelegationScenario.RoundStart:
                button.interactable = usableRoundStart;
                break;
            case DelegationCore.DelegationScenario.RoundEnd:
                button.interactable = usableRoundEnd;
                break;
            case DelegationCore.DelegationScenario.TimeScale:
                button.interactable = usableDuringTimeScaleSpeeds;
                break;
            case DelegationCore.DelegationScenario.Counter:
                button.interactable = usableCounterSpeed;
                break;
        }
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}