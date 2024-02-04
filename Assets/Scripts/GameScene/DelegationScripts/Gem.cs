using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour, IDelegationAction
{
    // Assigned in prefab:
    [SerializeField] private Button button;

    // Assigned in scene:
    [SerializeField] private DelegationCore delegationCore;

    public bool IsTargeted { get; private set; }

    [NonSerialized] public Elemental elemental;

    private bool hasGem;

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
        if (!hasGem) return;

        if (delegationScenario == DelegationCore.DelegationScenario.Reset)
            button.interactable = false;
        else if (elemental.Health != elemental.MaxHealth)
            button.interactable = true;
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}