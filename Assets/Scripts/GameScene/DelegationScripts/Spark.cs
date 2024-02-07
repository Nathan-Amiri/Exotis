using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Spark : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentElemental;

    [SerializeField] private Button button;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
    public bool IsTargeted { get; private set; }

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
        IsTargeted = true;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!parentElemental.isAlly)
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