using UnityEngine;

[CreateAssetMenu(fileName = "SpellInfo", menuName = "ScriptableObjects/SpellInfo")]
public class SpellInfo : ScriptableObject
{
    public enum ElementColor { water, flame, earth, wind, lightning, frost, shadow, venom, jewel }
    public ElementColor elementColor;

    public char timeScale;
}