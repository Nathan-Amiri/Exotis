using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDelegationAction
{
    bool IsTargeted { get; }
    void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario);
}