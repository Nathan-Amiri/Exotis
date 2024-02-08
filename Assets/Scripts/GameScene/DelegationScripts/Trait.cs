using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Trait : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentReference;

    [SerializeField] private Button button;
        // Accessed by Elemental
    public TMP_Text nameText;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
        // IDelegationAction fields:
    public Elemental ParentElemental { get; set; }
    public bool IsTargeted { get; private set; }    
    public bool CanTargetSelf { get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }

    private bool usableRoundStart;
    private bool usableRoundEnd;
    private bool usableCounterSpeed;
    private bool usableDuringTimeScaleSpeeds;

    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    // Called by Elemental
    public void SetElementalInfoFields(ElementalInfo info)
    {
        ParentElemental = parentReference;
        IsTargeted = info.traitIsTargeted;
        CanTargetSelf = info.traitCanTargetSelf;
        CanTargetAlly = info.traitCanTargetAlly;
        CanTargetEnemy = info.traitCanTargetEnemy;
        CanTargetBenchedAlly = info.traitCanTargetBenchedAlly;

        usableRoundStart = info.usableRoundStart;
        usableRoundEnd = info.usableRoundEnd;
        usableDuringTimeScaleSpeeds = info.usableDuringTimeScaleSpeeds;
        usableCounterSpeed = info.usableCounterSpeed;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!ParentElemental.isAlly)
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