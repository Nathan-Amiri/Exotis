using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Elemental : MonoBehaviour
{
    // PREFAB REFERENCE:
    [SerializeField] private GameObject extra; // Everything but display. Set inactive when benched
    [SerializeField] private GameObject status; // Flip if enemy
    [SerializeField] private List<Image> colorOutlines;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image speedColorBackground;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image icon; // Set a to 150 when targeting
    [SerializeField] private GameObject targetButton;
    [SerializeField] private Image retreatButton;

    // SCENE REFERENCE:
    [SerializeField] private Trait trait;

    // DYNAMIC:
        // Set by Setup, read by Trait/Spell, true if this Elemental is on the local player's team
    [NonSerialized] public bool isAlly;
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }

    [NonSerialized] public bool hasSpark;
    [NonSerialized] public bool hasGem;
    [NonSerialized] public bool hasPotion;

    [NonSerialized] public bool isDisengaged;
    [NonSerialized] public bool isTrapped;

    // Called by Setup
    public void Setup(string elementalName)
    {
        name = elementalName;
        nameText.text = elementalName;
        icon.sprite = Resources.Load<Sprite>("ElementalSprites/" + elementalName);

        ElementalInfo info = Resources.Load<ElementalInfo>("ElementalInfos/" + elementalName);

        if (info.speed == ElementalInfo.Speed.fast)
        {
            speedColorBackground.color = StaticLibrary.gameColors["fastHealthBack"];
            healthText.text = "5";
        }
        else if (info.speed == ElementalInfo.Speed.medium)
        {
            speedColorBackground.color = StaticLibrary.gameColors["mediumHealthBack"];
            healthText.text = "6";
        }
        else // If slow
        {
            speedColorBackground.color = StaticLibrary.gameColors["slowHealthBack"];
            healthText.text = "7";
        }

        // If in slot 4 5 6 or 7, is benched
        if (SlotAssignment.GetSlot(this) > 3)
            ToggleBenched(true);

        foreach (Image outline in colorOutlines)
            outline.color = isAlly ? StaticLibrary.gameColors["allyOutline"] : StaticLibrary.gameColors["enemyOutline"];

        retreatButton.color = isAlly ? StaticLibrary.gameColors["pink"] : StaticLibrary.gameColors["gray"];

        // Trait setup
        trait.nameText.text = info.traitName;
        trait.SetElementalInfoFields(info);
    }

    public void ToggleBenched(bool benched)
    {
        extra.SetActive(!benched);
    }
}