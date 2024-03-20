using System;
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
    [SerializeField] private SlotAssignment slotAssignment;

    [SerializeField] private Transform inPlayParent;
    [SerializeField] private Transform benchParent;

    // DYNAMIC:
        // Set by Setup, read by Trait/Spell, true if this Elemental is on the local player's team
    [NonSerialized] public bool isAlly;
    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
        // 0 = slow, 1 = medium, 2 = fast. When Slowed, Speed -= 3
    public int Speed { get; private set; }

    [NonSerialized] public int currentActions;

        // Only true during SpellEffect when Spell is boosted by Potion/Frenzy
    [NonSerialized] public bool potionBoosting;
    [NonSerialized] public bool frenzyBoosting; // *Frenzy

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

    [NonSerialized] public bool cannotSwapIn; // *Mirage

    [NonSerialized] public bool hasCastSurge; // *Surge
    [NonSerialized] public bool hasCastHex; // *Hex

    [NonSerialized] public bool isEmpowered; // *Empower, //.make into status condition

    [NonSerialized] public bool inPoisonCloud; // *Poison Cloud, //.make into status condition
    public readonly List<Elemental> poisonedByPoisonCloud = new();

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

        if (!benched)
            OnSwapIntoPlay();
    }


    public void DealDamage(int amount, Elemental caster, bool spellDamage = true)
    {
        if (EnrageStrength > 0)
            return;

        if (DisengageStrength > 0)
            return;

        if (spellDamage)
        {
            if (caster.potionBoosting)
            {
                amount += 1;
                caster.TogglePotion(false);
            }

            if (caster.frenzyBoosting) // *Frenzy
                amount += 1;

            if (ArmorStrength > 0)
            {
                amount -= 1;
                ToggleArmored(false);
            }

            if (caster.WeakenStrength > 0)
                amount -= 1;

            if (caster.inPoisonCloud && amount > 0) // *Poison Cloud
            {
                TogglePoisoned(true);
                caster.poisonedByPoisonCloud.Add(this);
            }
        }

        if (amount <= 0)
            return;

        isEmpowered = true;

        Health -= amount;
    }

    public void Heal(int amount)
    {
        Health += amount;
    }
    public void ApplyHealthChange()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);

        healthText.text = Health.ToString();
    }

    public void Eliminate()
    {
        Health = 0;

        healthText.text = "0";
    }

    public bool CanSwapOut()
    {
        if (TrapStrength > 0)
            return false;

        // Check if any benched allies exist
        int guestAdd = NetworkManager.Singleton.IsHost ? 0 : 2;

        Elemental benchedElemental1 = slotAssignment.Elementals[4 + guestAdd];
        if (benchedElemental1 != null && benchedElemental1.CanSwapIn())
            return true;
        Elemental benchedElemental2 = slotAssignment.Elementals[5 + guestAdd];
        if (benchedElemental2 != null && benchedElemental2.CanSwapIn())
            return true;

        return false;
    }
    public bool CanSwapIn() // *Mirage
    {
        return !cannotSwapIn;
    }
    public bool AllyCanSwapOut() // Flurry
    {
        return slotAssignment.GetAlly(this).CanSwapOut();
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

    public Spell GetSpell(string spellName)
    {
        foreach (Spell spell in spells)
            if (spell.Name == spellName)
                return spell;

        Debug.LogError("The following Spell was not found among " + name + "'s Spells: " + spellName);
        return null;
    }

    public void OnRoundStart()
    {
        isEmpowered = false;
    }
    public void OnRoundEnd()
    {
        foreach (Spell spell in spells)
            if (spell.readyForRecast)
                spell.ToggleRecast(false);

        ToggleArmored(false);
    }
    public void OnSwapIntoPlay()
    {
        foreach (Spell spell in spells)
            spell.cannotCastUntilSwap = false;
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
        if (ArmorStrength == 0 && !becomeArmored)
            return;

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
        if (becomeSlowed && SlowStrength == 0)
            Speed -= 3;
        else if (!becomeSlowed && SlowStrength > 0)
            Speed += 3;

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