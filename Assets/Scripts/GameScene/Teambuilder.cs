using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Teambuilder : MonoBehaviour
{
    //temporary class for testing

    [SerializeField] private List<Elemental> sceneElementals = new();
    [SerializeField] private List<Spell> sceneSpells = new();

    [SerializeField] private GameObject teambuilderCanvas;

    [SerializeField] private List<TMP_InputField> elementalInputFields = new();
    [SerializeField] private List<TMP_InputField> spellInputFields = new();

    private void Start()
    {
        List<string> teamElementalNames = new();
        List<string> teamSpellNames = new();

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

        //setup:
        for (int i = 0; i < teamElementalNames.Count; i++)
            sceneElementals[i].Setup(teamElementalNames[i]);

        for (int i = 0; i < teamSpellNames.Count; i++)
            sceneSpells[i].Setup(teamSpellNames[i]);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            teambuilderCanvas.SetActive(!teambuilderCanvas.activeSelf);
    }

    public void SelectSave()
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

        for (int i = 0; i < elementalInputFields.Count; i++)
            PlayerPrefs.SetString("Elemental" + i, elementalInputFields[i].text);

        for (int i = 0; i < spellInputFields.Count; i++)
            PlayerPrefs.SetString("Spell" + i, spellInputFields[i].text);

        Debug.Log("team saved successfully! Restart playmode");
    }
}