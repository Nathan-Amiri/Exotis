using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trait : MonoBehaviour, IDelegationAction
{
    public bool IsTargeted { get; private set; }

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
        
    }
}