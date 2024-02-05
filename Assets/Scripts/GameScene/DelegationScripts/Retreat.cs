using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Retreat : MonoBehaviour, IDelegationAction
{
    // Assigned in prefab:
    [SerializeField] private Button button;

    // Assigned in scene:
    [SerializeField] private DelegationCore delegationCore;

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

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
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