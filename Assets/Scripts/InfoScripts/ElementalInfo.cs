using UnityEngine;

[CreateAssetMenu(fileName = "ElementalInfo", menuName = "ScriptableObjects/ElementalInfo")]
public class ElementalInfo : ScriptableObject
{
    public enum Speed { fast, medium, slow }
    public Speed speed;
}