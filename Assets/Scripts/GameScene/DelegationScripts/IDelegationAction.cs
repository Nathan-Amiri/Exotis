
using UnityEditor;

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

    void OnNewActionNeeded(bool reset = false);
}