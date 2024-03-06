using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Spark : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentReference;

    [SerializeField] private Button button;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
    public string ActionType { get; private set; }
    public Elemental ParentElemental { get; set; }
    public int MaxTargets { get; private set; }
    public bool CanTargetSelf { get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }
    public string Name { get; private set; }


    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    private void Awake()
    {
        ActionType = "spark";
        ParentElemental = parentReference;
        MaxTargets = 1;
        CanTargetSelf = true;
        CanTargetAlly = true;
        CanTargetEnemy = true;
        CanTargetBenchedAlly = false;
        // IDelegationAction Name is unnecessary, as it is used only for Spell/Trait
    }

    public void OnNewActionNeeded(bool reset = false)
    {
        if (!ParentElemental.isAlly)
            return;

        if (!ParentElemental.hasSpark)
            return;

        if (reset)
            button.interactable = false;
        else if (Clock.CurrentRoundState == Clock.RoundState.Counter)
            button.interactable = true;
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}