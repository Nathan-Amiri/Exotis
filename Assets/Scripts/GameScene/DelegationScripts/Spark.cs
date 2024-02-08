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
    public Elemental ParentElemental { get; set; }
    public bool IsTargeted { get; private set; }
    public bool CanTargetSelf { get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }

    private bool hasSpark;

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
        CanTargetEnemy = true;
        CanTargetBenchedAlly = false;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!ParentElemental.isAlly)
            return;

        if (delegationScenario == DelegationCore.DelegationScenario.Reset)
            button.interactable = false;
        else if (delegationScenario == DelegationCore.DelegationScenario.Counter)
            button.interactable = true;
    }

    public void OnClick()
    {
        if (!hasSpark)
            return;

        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}