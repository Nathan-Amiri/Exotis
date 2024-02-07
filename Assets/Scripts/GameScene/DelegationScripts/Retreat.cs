using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Retreat : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentElemental;

    [SerializeField] private Button button;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
    public bool IsTargeted { get; private set; }

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
        IsTargeted = true;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!parentElemental.isAlly)
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