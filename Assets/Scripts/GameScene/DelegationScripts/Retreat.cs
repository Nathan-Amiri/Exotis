using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Retreat : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentReference;

    [SerializeField] private Button button;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
    public Elemental ParentElemental { get; set; }
    public bool IsTargeted { get; private set; }
    public bool CanTargetSelf { get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }

    [NonSerialized] public Elemental elemental;

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
        ParentElemental = parentReference;
        IsTargeted = true;
        CanTargetSelf = false;
        CanTargetAlly = false;
        CanTargetEnemy = false;
        CanTargetBenchedAlly = true;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!ParentElemental.isAlly)
            return;

        //if elemental is trapped or first benched elemental isn't in slot assignment (no available swap), interactable = false, then return

        button.interactable = delegationScenario != DelegationCore.DelegationScenario.Reset;
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }

}