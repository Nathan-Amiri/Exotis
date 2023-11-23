using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDeclarable
{
    bool IsTargeted { get; }
    void OnNewDelegation(DelegationCore.DelegationScenario delegationScenario);
}