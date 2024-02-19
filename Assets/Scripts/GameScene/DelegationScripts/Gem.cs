using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour, IDelegationAction
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
        ActionType = "gem";
        ParentElemental = parentReference;
        MaxTargets = 0;
        // IDelegationAction target bools are unnecessary
        // IDelegationAction Name is unnecessary, as it is used only for Spell/Trait
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!ParentElemental.isAlly)
            return;

        if (!ParentElemental.hasGem)
            return;

        if (delegationScenario == DelegationCore.DelegationScenario.Reset)
            button.interactable = false;
        else if (ParentElemental.Health != ParentElemental.MaxHealth)
            button.interactable = true;
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}