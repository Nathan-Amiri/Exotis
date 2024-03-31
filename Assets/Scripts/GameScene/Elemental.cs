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
    [SerializeField] private ExecutionCore executionCore;

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

    [NonSerialized] public bool readyForElimination;

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

    public int MirageStrength { get; private set; } // *Mirage
    public int DeceiveStrength { get; private set; } // *Deceive
    public int EmpowerStrength { get; private set; } // *Empower
    public int NumbStrength { get; private set; } // *Numb // *Best Wishes
    public int CloudStrength { get; private set; } // *Poison Cloud


    [NonSerialized] public Elemental mirageRedirectTarget; // *Mirage
    [NonSerialized] public Elemental deceiveRedirectTarget; // *Deceive
    public readonly List<Elemental> poisonedByPoisonCloud = new();
    [NonSerialized] public Elemental trappedByParalyze; // *Paralyze
    [NonSerialized] public bool permanentlySlowed; // *Icy Touch
    [NonSerialized] public Elemental gargoyleDamager; // *Scavenger
    [NonSerialized] public bool mysticArtsActive; // *Mystic Arts

    public void Setup(string elementalName) // Called by Setup
    {
        name = elementalName;
        nameText.text = elementalName;
        icon.sprite = Resources.Load<Sprite>("ElementalSprites/" + elementalName);

        ElementalInfo info = Resources.Load<ElementalInfo>("ElementalInfos/" + elementalName);

        if (info.speed == ElementalInfo.Speed.fast)
        {
            speedColorBackground.color = StaticLibrary.gameColors["fastHealthBack"];
            MaxHealth = 6;
            Speed = 3;
        }
        else if (info.speed == ElementalInfo.Speed.medium)
        {
            speedColorBackground.color = StaticLibrary.gameColors["mediumHealthBack"];
            MaxHealth = 7;
            Speed = 2;
        }
        else // If slow
        {
            speedColorBackground.color = StaticLibrary.gameColors["slowHealthBack"];
            MaxHealth = 8;
            Speed = 1;
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
        else
            OnSwapOutOfPlay();
    }


    public void DealDamage(int amount, EffectInfo info, bool eruptRecoil = false)
    {
        bool spellDamage = info.isSpell;

        if (name == "Will-o'-Wisp" && slotAssignment.GetAlly(this) == info.caster) // *Carefree
            return;

        if (deceiveRedirectTarget != null) // *Deceive
        {
            deceiveRedirectTarget.DealDamage(amount, info);
            return;
        }

        if (mirageRedirectTarget != null) // *Mirage
        {
            mirageRedirectTarget.DealDamage(amount, info);
            return;
        }

        if (EnrageStrength > 0)
            return;

        if (DisengageStrength > 0)
            return;

        if (spellDamage)
        {
            if (info.caster.NumbStrength > 0) // *Numbing Cold // *Best Wishes
                return;

            if (info.potionBoosting && !eruptRecoil) // *Erupt
            {
                amount += 1;
                info.caster.TogglePotion(false);
            }

            if (info.traitBoosting && !eruptRecoil)
                amount += 1;

            if (ArmorStrength > 0)
            {
                amount -= 1;
                ToggleArmored(false);
            }

            if (info.caster.WeakenStrength > 0)
                amount -= 1;

            if (CloudStrength > 0 && amount > 0 && info.caster.isAlly != isAlly) // *Poison Cloud
            {
                info.caster.TogglePoisoned(true);
                poisonedByPoisonCloud.Add(info.caster);
            }
        }

        if (amount <= 0)
            return;

        if (name == "Griffin" && !spellDamage && !trait.hasOccurredThisRound) // *Nimble
        {
            trait.hasOccurredThisRound = true;

            return;
        }

        if (info.caster.name == "Dragon" && spellDamage && info.caster.isAlly != isAlly && !info.caster.trait.hasOccurredThisRound) // *Devour
        {
            info.caster.trait.hasOccurredThisRound = true;

            info.caster.Heal(1);
        }
        
        if (name == "Angel" && spellDamage && !trait.hasOccurredThisRound) // *Smite
        {
            trait.hasOccurredThisRound = true;

            ToggleSpark(true);
        }

        if (info.caster.name == "Gargoyle" && info.caster.isAlly != isAlly) // *Scavenger
            gargoyleDamager = info.caster;

        if (EmpowerStrength == 0 && Clock.CurrentRoundState != Clock.RoundState.RoundEnd)
            foreach (Spell spell in spells) // *Empower
                if (spell.Name == "Empower")
                    ToggleEmpowered(true);

        Health -= amount;
    }

    public void Heal(int amount)
    {
        Health += amount;

        if (name == "Fairy") // *Serenade
            trait.hasOccurredThisRound = true;
    }
    public void ApplyHealthChange()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);

        healthText.text = Health.ToString();

        if (gargoyleDamager != null) // *Scavenger
        {
            if (Health == 0 && (name != "Phoenix" || trait.hasOccurredThisGame)) // *Rebirth
                gargoyleDamager.TogglePotion(true);

            gargoyleDamager = null;
        }
    }

    public void Eliminate()
    {
        if (name == "Phoenix" && !trait.hasOccurredThisGame) // *Rebirth
        {
            trait.hasOccurredThisGame = true;

            Health = 1;
            healthText.text = "1";

            ToggleWeakened(true);
            return;
        }
        
        if (name == "Scorpio" && trappedByParalyze != null) // *Paralyze
            trappedByParalyze.ToggleTrapped(false);

        // Remove from Elementals manually since Destroy doesn't occur until the end of the frame
        for (int i = 0; i < slotAssignment.Elementals.Count; i++)
            if (slotAssignment.Elementals[i] == this)
                slotAssignment.Elementals[i] = null;

        Destroy(gameObject);
    }

    public bool CanSwapOut()
    {
        if (TrapStrength > 0)
            return false;

        // Check if any benched allies exist
        int a = NetworkManager.Singleton.IsHost ? 0 : 2;

        if (slotAssignment.Elementals[4 + a] != null)
            return true;
        if (slotAssignment.Elementals[5 + a] != null)
            return true;

        return false;
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
        trait.occurredLastRound = trait.hasOccurredThisRound;
        trait.hasOccurredThisRound = false;

        if (name == "Kraken" && !trait.hasOccurredThisGame) // Reservoir
        {
            trait.hasOccurredThisGame = true;

            ToggleGem(true);
        }
        else if (name == "Wizard" && slotAssignment.GetSlot(this) < 4 && !trait.hasOccurredThisGame) // *Astonish
        {
            trait.hasOccurredThisGame = true;

            ToggleEnraged(true);
            EffectInfo info = new()
            {
                spellOrTraitName = "Astonish",
                caster = this
            };
            executionCore.AddRoundEndDelayedEffect(0, info);
        }
        else if (name == "Ogre" && !trait.hasOccurredThisGame) // *Juggernaut
        {
            trait.hasOccurredThisGame = true;

            ToggleSlowed(true);
        }
    }
    public void OnRoundEnd()
    {
        ToggleArmored(false);

        ToggleEmpowered(false);

        if (name == "Fairy" && !trait.hasOccurredThisRound) // *Serenade
        {
            Heal(1);
            ApplyHealthChange();
        }
    }
    public void OnSwapIntoPlay()
    {
        foreach (Spell spell in spells)
            spell.cannotCastUntilSwap = false;
    }
    public void OnSwapOutOfPlay()
    {
        if (name == "Scorpio" && trappedByParalyze != null) // *Paralyze
        {
            trappedByParalyze.ToggleTrapped(false);
            trappedByParalyze = null;
        }
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
    public void ToggleSlowed(bool becomeSlowed, bool icyTouchSlow = false) // *Icy Touch
    {
        if (permanentlySlowed)
            return;
        else if (icyTouchSlow)
            permanentlySlowed = true;

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

    public void ToggleMiraged(bool becomeMiraged) // *Mirage
    {
        UpdateStatusIcons(9, MirageStrength, becomeMiraged);
        MirageStrength += becomeMiraged ? 1 : -1;
    }
    public void ToggleDeceived(bool becomeDeceived) // *Deceive
    {
        UpdateStatusIcons(10, DeceiveStrength, becomeDeceived);
        DeceiveStrength += becomeDeceived ? 1 : -1;
    }
    public void ToggleEmpowered(bool becomeEmpowered) // *Empower
    {
        if (EmpowerStrength == 0 && !becomeEmpowered)
            return;

        UpdateStatusIcons(11, EmpowerStrength, becomeEmpowered);
        EmpowerStrength += becomeEmpowered ? 1 : -1;
    }
    public void ToggleNumb(bool becomeNumb) // *Numbing Cold
    {
        UpdateStatusIcons(12, NumbStrength, becomeNumb);
        NumbStrength += becomeNumb ? 1 : -1;
    }
    public void ToggleWished(bool becomeWished) // *Best Wishes
    {
        // Use NumbStrength since the effect is identical
        UpdateStatusIcons(13, NumbStrength, becomeWished);
        NumbStrength += becomeWished ? 1 : -1;
    }
    public void ToggleClouded(bool becomeClouded) // *Numbing Cold
    {
        UpdateStatusIcons(14, CloudStrength, becomeClouded);
        CloudStrength += becomeClouded ? 1 : -1;
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