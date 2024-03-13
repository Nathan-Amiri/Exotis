using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

public class Teambuilder : MonoBehaviour
{
    //.temporary class for testing

    [SerializeField] private TMP_Text errorText;

    [SerializeField] private List<TMP_InputField> elementalInputFields = new();
    [SerializeField] private List<TMP_InputField> spellInputFields = new();

    // Read by Setup:
    [NonSerialized] public List<string> teamElementalNames = new();
    [NonSerialized] public List<string> teamSpellNames = new();

    private void Awake()
    {
        for (int i = 0; i < elementalInputFields.Count; i++)
        {
            if (!PlayerPrefs.HasKey("Elemental" + i))
                return;

            teamElementalNames.Add(PlayerPrefs.GetString("Elemental" + i));
            elementalInputFields[i].text = teamElementalNames[i];
        }

        for (int i = 0; i < spellInputFields.Count; i++)
        {
            if (!PlayerPrefs.HasKey("Spell" + i))
                return;

            teamSpellNames.Add(PlayerPrefs.GetString("Spell" + i));
            spellInputFields[i].text = teamSpellNames[i];
        }
    }

    public void SelectSave()
    {
        foreach (var field in elementalInputFields)
            if (!StaticLibrary.validElementalNames.Contains(field.text))
            {
                StartCoroutine(ErrorMessage("The following Elemental name is invalid: " + field.text));
                return;
            }

        foreach (var field in spellInputFields)
            if (!StaticLibrary.validSpellNames.Contains(field.text))
            {
                StartCoroutine(ErrorMessage("The following Spell name is invalid: " + field.text));
                return;
            }

        for (int i = 0; i < elementalInputFields.Count; i++)
            PlayerPrefs.SetString("Elemental" + i, elementalInputFields[i].text);

        for (int i = 0; i < spellInputFields.Count; i++)
            PlayerPrefs.SetString("Spell" + i, spellInputFields[i].text);

        StartCoroutine(ErrorMessage("Team saved successfully!"));
    }

    private IEnumerator ErrorMessage(string newMessage) //.temporary duplicate of gamemanager's ErrorMessage method
    {
        errorText.text = newMessage;

        yield return new WaitForSeconds(3);

        if (errorText.text == newMessage)
            errorText.text = string.Empty;
    }
}