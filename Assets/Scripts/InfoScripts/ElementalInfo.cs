using UnityEngine;

[CreateAssetMenu(fileName = "ElementalInfo", menuName = "ScriptableObjects/ElementalInfo")]
public class ElementalInfo : ScriptableObject
{
    public enum Speed { fast, medium, slow }
    public Speed speed;

    public string traitName;

    public int traitMaxTargets;
    public bool traitCanTargetSelf;
    public bool traitCanTargetAlly;
    public bool traitCanTargetEnemy;
    public bool traitCanTargetBenchedAlly;

    public bool usableRoundStart;
    public bool usableRoundEnd;
    public bool usableCounterSpeed;
    public bool usableDuringTimescaleSpeeds;
}