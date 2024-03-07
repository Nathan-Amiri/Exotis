using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
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

    [SerializeField] private Image gemImage;
    [SerializeField] private Sprite gemSprite;
    [SerializeField] private Sprite gemGraySprite;

    [SerializeField] private Image sparkImage;
    [SerializeField] private Sprite sparkSprite;
    [SerializeField] private Sprite sparkGraySprite;

    [SerializeField] private Image potionImage;
    [SerializeField] private Sprite potionSprite;
    [SerializeField] private Sprite potionGraySprite;

    // Read by ExecutionCore
    public Trait trait;
    public List<Spell> spells = new();

    // SCENE REFERENCE:
    [SerializeField] private Transform inPlayParent;
    [SerializeField] private Transform benchParent;

    // DYNAMIC:
        // Set by Setup, read by Trait/Spell, true if this Elemental is on the local player's team
    [NonSerialized] public bool isAlly;
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }

    [NonSerialized] public int currentActions;

    [NonSerialized] public bool hasSpark;
    [NonSerialized] public bool hasGem;
    [NonSerialized] public bool hasPotion;

    [NonSerialized] public bool isDisengaged;
    [NonSerialized] public bool isTrapped;
    [NonSerialized] public bool isWearied;

    public void Setup(string elementalName) // Called by Setup
    {
        name = elementalName;
        nameText.text = elementalName;
        icon.sprite = Resources.Load<Sprite>("ElementalSprites/" + elementalName);

        ElementalInfo info = Resources.Load<ElementalInfo>("ElementalInfos/" + elementalName);

        if (info.speed == ElementalInfo.Speed.fast)
        {
            speedColorBackground.color = StaticLibrary.gameColors["fastHealthBack"];
            MaxHealth = 5;
        }
        else if (info.speed == ElementalInfo.Speed.medium)
        {
            speedColorBackground.color = StaticLibrary.gameColors["mediumHealthBack"];
            MaxHealth = 6;
        }
        else // If slow
        {
            speedColorBackground.color = StaticLibrary.gameColors["slowHealthBack"];
            MaxHealth = 7;
        }

        Health = MaxHealth;
        healthText.text = Health.ToString();

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
        transform.SetParent(benched ? benchParent : inPlayParent);
        transform.localScale = benched ? new Vector2(.5f, .5f) : Vector2.one;
    }


    public void TakeDamage(int amount)
    {

        HealthChange(-amount);
    }

    public void HealthChange(int amount)
    {
        Health += amount;
        Health = Mathf.Clamp(Health, 0, MaxHealth);

        healthText.text = Health.ToString();
    }

    public bool CanSwap()
    {
        if (isTrapped)
            return false;

        // Check if any benched allies exist
        int guestAdd = NetworkManager.Singleton.IsHost ? 0 : 2;
        if (SlotAssignment.Elementals[4 + guestAdd] == null && SlotAssignment.Elementals[5 + guestAdd] == null)
            return false;

        return true;
    }

    public void ToggleGem(bool gainGem)
    {
        hasGem = gainGem;

        gemImage.sprite = gainGem ? gemSprite : gemGraySprite;
    }
    public void ToggleSpark(bool gainSpark)
    {
        hasSpark = gainSpark;

        sparkImage.sprite = gainSpark ? sparkSprite : sparkGraySprite;
    }
    public void TogglePotion(bool gainPotion)
    {
        hasPotion = gainPotion;

        potionImage.sprite = gainPotion ? potionSprite : potionGraySprite;
    }
}