using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDelegationAction
{
    Elemental ParentElemental { get; }
    bool IsTargeted { get; }
    bool CanTargetSelf { get; }
    bool CanTargetAlly { get; }
    bool CanTargetEnemy { get; }
    bool CanTargetBenchedAlly { get; }

    void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario);
}