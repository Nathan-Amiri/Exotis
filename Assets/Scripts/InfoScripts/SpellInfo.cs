using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellInfo", menuName = "ScriptableObjects/SpellInfo")]
public class SpellInfo : ScriptableObject
{
    public enum ElementColor { water, flame, earth, wind, lightning, frost, shadow, venom, jewel }
    public ElementColor elementColor;

    public char timeScale;

    public bool isTargeted;
    public bool canTargetSelf;
    public bool canTargetAlly;
    public bool canTargetEnemy;
    public bool canTargetBenchedAlly;
}