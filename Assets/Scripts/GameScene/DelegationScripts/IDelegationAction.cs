using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDelegationAction
{
    string ActionType { get; }
    Elemental ParentElemental { get; }
    int MaxTargets { get; }
    bool CanTargetSelf { get; }
    bool CanTargetAlly { get; }
    bool CanTargetEnemy { get; }
    bool CanTargetBenchedAlly { get; }

    // Name only used for Spells and Traits
    string Name { get; }

    void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario);
}