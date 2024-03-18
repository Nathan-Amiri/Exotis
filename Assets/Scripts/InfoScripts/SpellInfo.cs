using UnityEngine;

[CreateAssetMenu(fileName = "SpellInfo", menuName = "ScriptableObjects/SpellInfo")]
public class SpellInfo : ScriptableObject
{
    public enum ElementColor { water, flame, earth, wind, lightning, frost, shadow, venom, jewel }
    public ElementColor elementColor;

    public char timescale;

    public int maxTargets;
    public bool canTargetSelf;
    public bool canTargetAlly;
    public bool canTargetEnemy;
    public bool canTargetBenchedAlly;

    public bool isDamaging;
    public bool isWearying;

    public int maxRecastTargets;
    public bool recastIsDamaging;
}