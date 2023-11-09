using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Teambuilder : MonoBehaviour
{
    [SerializeField] private List<TMP_InputField> elementalInputFields = new();
    [SerializeField] private List<TMP_InputField> spellInputFields = new();

    public void SelectReady()
    {
        foreach (var field in elementalInputFields)
            if (!StaticLibrary.validElementalNames.Contains(field.text))
            {
                Debug.Log("Team not valid!");
                return;
            }

        foreach (var field in spellInputFields)
            if (!StaticLibrary.validSpellNames.Contains(field.text))
            {
                Debug.Log("Team not valid!");
                return;
            }

        Team newTeam = new()
        {
            Elemental0 = elementalInputFields[0].text,
            Spell00 = spellInputFields[0].text,
            Spell01 = spellInputFields[1].text,
            Spell02 = spellInputFields[2].text,

            Elemental1 = elementalInputFields[1].text,
            Spell10 = spellInputFields[3].text,
            Spell11 = spellInputFields[4].text,
            Spell12 = spellInputFields[5].text,

            Elemental2 = elementalInputFields[2].text,
            Spell20 = spellInputFields[6].text,
            Spell21 = spellInputFields[7].text,
            Spell22 = spellInputFields[8].text,

            Elemental3 = elementalInputFields[3].text,
            Spell30 = spellInputFields[9].text,
            Spell31 = spellInputFields[10].text,
            Spell32 = spellInputFields[11].text,
        };
    }
}
public struct Team
{
    public string Elemental0;
    public string Spell00;
    public string Spell01;
    public string Spell02;

    public string Elemental1;
    public string Spell10;
    public string Spell11;
    public string Spell12;

    public string Elemental2;
    public string Spell20;
    public string Spell21;
    public string Spell22;

    public string Elemental3;
    public string Spell30;
    public string Spell31;
    public string Spell32;
}