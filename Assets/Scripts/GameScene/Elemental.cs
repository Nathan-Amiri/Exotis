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
    [SerializeField] private GameObject extra;
    [SerializeField] private GameObject status;
    [SerializeField] private List<Image> colorOutlines;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image speedColorBackground;
    [SerializeField] private TMP_Text healthText;
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

        // IDelegationActions. Read by ExecutionCore
    public Trait trait;
    public List<Spell> spells = new();
    public Retreat retreat;
    public Gem gem;
    public Spark spark;

        // Set by TargetManager
    public Image icon;
    public Button targetButton;

    // SCENE REFERENCE:
    [SerializeField] private Transform inPlayParent;
    [SerializeField] private Transform benchParent;

    // DYNAMIC:
        // Set by Setup, read by Trait/Spell, true if this Elemental is on the local player's team
    [NonSerialized] public bool isAlly;
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }

    [NonSerialized] public int currentActions;

        // Only true during SpellEffect when Spell is boosted by Potion/Frenzy
    [NonSerialized] public bool potionBoosting;
    [NonSerialized] public bool frenzyBoosting;

        // Items
    public bool HasSpark { get; private set; }
    public bool HasGem { get; private set; }
    public bool HasPotion { get; private set; }

        // Status
    [SerializeField] private List<StatusIcon> statusIcons = new();
    private readonly List<int> currentStatuses = new();

        // If wearyStrength > 0, isWearied. Strength is increased +1 when status is applied
        // Strength ensures that multiple sources don't conflict when applying the status for different durations
    public int WearyStrength { get; private set; }
    public int EnrageStrength { get; private set; }
    public int ArmorStrength { get; private set; }
    public int DisengageStrength { get; private set; }
    public int StunStrength { get; private set; }
    public int SlowStrength { get; private set; }
    public int TrapStrength { get; private set; }
    public int PoisonStrength { get; private set; }
    public int WeakenStrength { get; private set; }


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

        // If guest, flip y position of Elemental and status icons
        if (!NetworkManager.Singleton.IsHost)
        {
            transform.localPosition *= new Vector2(1, -1);
            status.transform.localPosition *= new Vector2(1, -1);
        }

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


    public void TakeDamage(int amount, Elemental caster, bool spellDamage = true)
    {
        if (caster.potionBoosting)
        {
            amount += 1;
            caster.TogglePotion(false);
        }

        if (caster.frenzyBoosting)
            amount += 1;

        //.weaken and armor only work on spelldamage

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
        if (TrapStrength > 0)
            return false;

        // Check if any benched allies exist
        int guestAdd = NetworkManager.Singleton.IsHost ? 0 : 2;
        if (SlotAssignment.Elementals[4 + guestAdd] == null && SlotAssignment.Elementals[5 + guestAdd] == null)
            return false;

        return true;
    }

    public bool CanAct()
    {
        if (retreat.ActionAvailable())
            return true;

        if (trait.ActionAvailable())
            return true;

        foreach (Spell spell in spells)
            if (spell.ActionAvailable())
                return true;

        if (spark.ActionAvailable())
            return true;

        if (gem.ActionAvailable())
            return true;

        return false;
    }

    // Item:
    public void ToggleGem(bool gainGem)
    {
        HasGem = gainGem;

        gemImage.sprite = gainGem ? gemSprite : gemGraySprite;
    }
    public void ToggleSpark(bool gainSpark)
    {
        HasSpark = gainSpark;

        sparkImage.sprite = gainSpark ? sparkSprite : sparkGraySprite;
    }
    public void TogglePotion(bool gainPotion)
    {
        HasPotion = gainPotion;

        potionImage.sprite = gainPotion ? potionSprite : potionGraySprite;
    }

    // Toggle Status:
    public void ToggleWearied(bool becomeWearied)
    {
        UpdateStatusIcons(0, WearyStrength, becomeWearied);
        WearyStrength += becomeWearied ? 1 : -1;
    }
    public void ToggleEnraged(bool becomeEnraged)
    {
        UpdateStatusIcons(1, EnrageStrength, becomeEnraged);
        EnrageStrength += becomeEnraged ? 1 : -1;
    }
    public void ToggleArmored(bool becomeArmored)
    {
        UpdateStatusIcons(2, ArmorStrength, becomeArmored);
        ArmorStrength += becomeArmored ? 1 : -1;
    }
    public void ToggleDisengaged(bool becomeDisengaged)
    {
        UpdateStatusIcons(3, DisengageStrength, becomeDisengaged);
        DisengageStrength += becomeDisengaged ? 1 : -1;
    }
    public void ToggleStunned(bool becomeStunned)
    {
        UpdateStatusIcons(4, StunStrength, becomeStunned);
        StunStrength += becomeStunned ? 1 : -1;
    }
    public void ToggleSlowed(bool becomeSlowed)
    {
        UpdateStatusIcons(5, SlowStrength, becomeSlowed);
        SlowStrength += becomeSlowed ? 1 : -1;
    }
    public void ToggleTrapped(bool becomeTrapped)
    {
        UpdateStatusIcons(6, TrapStrength, becomeTrapped);
        TrapStrength += becomeTrapped ? 1 : -1;
    }
    public void TogglePoisoned(bool becomePoisoned)
    {
        UpdateStatusIcons(7, PoisonStrength, becomePoisoned);
        PoisonStrength += becomePoisoned ? 1 : -1;
    }
    public void ToggleWeakened(bool becomeWeakened)
    {
        UpdateStatusIcons(8, WeakenStrength, becomeWeakened);
        WeakenStrength += becomeWeakened ? 1 : -1;
    }

    // Update Status Icons:
    private void UpdateStatusIcons(int statusNumber, int oldStatusStrength, bool increaseStatusStrength)
    {
        // If status effect is being added
        if (oldStatusStrength == 0 && increaseStatusStrength)
        {
            currentStatuses.Add(statusNumber);

            // Get the next available status icon
            // 7 is the maximum number of Status Icons that can be active at once
            if (currentStatuses.Count <= 7)
                statusIcons[currentStatuses.Count - 1].AddIcon(statusNumber);
        }
        // If status effect is being removed
        else if (oldStatusStrength == 1 && !increaseStatusStrength)
            RemoveStatusIcon(statusNumber);
    }
    private void RemoveStatusIcon(int statusNumber)
    {
        // Get the position that status's status number is in
        if (!currentStatuses.Contains(statusNumber))
            Debug.LogError("Status not found");

        int statusIconPosition = currentStatuses.IndexOf(statusNumber);

        // Remove status number
        currentStatuses.RemoveAt(statusIconPosition);

        // Update status icons
        for (int i = 0; i < statusIcons.Count; i++)
        {
            if (currentStatuses.Count > i)
                statusIcons[i].AddIcon(currentStatuses[i]);
            else
                statusIcons[i].RemoveIcon();
        }
    }
}