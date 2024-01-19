using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementalInfo", menuName = "ScriptableObjects/ElementalInfo")]
public class ElementalInfo : ScriptableObject
{
    public enum Speed { fast, medium, slow }
    public Speed speed;

    public bool usableRoundStart;
    public bool usableRoundEnd;
    public bool usableCounterSpeed;
    public bool usableDuringTimeScaleSpeeds;
}