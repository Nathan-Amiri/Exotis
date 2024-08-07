using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Trait : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentReference;

    [SerializeField] private Button button;

        // Accessed by Elemental
    public TMP_Text nameText;

    // SCENE REFERENCE:
    [SerializeField] private DelegationCore delegationCore;
    [SerializeField] private SlotAssignment slotAssignment;

    // DYNAMIC:
        // IDelegationAction fields:
    public string ActionType { get; private set; }
    public Elemental ParentElemental { get; set; }
    public int MaxTargets { get; private set; }
    public bool CanTargetSelf { get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }
    public string Name { get; private set; }

    public bool OncePerGame { get; private set; }
    public bool OncePerRound { get; private set; }

    [NonSerialized] public bool hasOccurredThisGame;
    [NonSerialized] public bool hasOccurredThisRound;
    [NonSerialized] public bool occurredLastRound;


    private bool usableRoundStart;
    private bool usableRoundEnd;
    private bool usableCounterSpeed;
    private bool usableDuringTimescaleSpeeds;

    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    public void SetElementalInfoFields(ElementalInfo info) // Called by Elemental
    {
        ActionType = "trait";
        ParentElemental = parentReference;
        MaxTargets = info.traitMaxTargets;
        CanTargetSelf = info.traitCanTargetSelf;
        CanTargetAlly = info.traitCanTargetAlly;
        CanTargetEnemy = info.traitCanTargetEnemy;
        CanTargetBenchedAlly = info.traitCanTargetBenchedAlly;
        Name = info.traitName;

        OncePerGame = info.traitOncePerGame;
        OncePerRound = info.traitOncePerRound;

        usableRoundStart = info.usableRoundStart;
        usableRoundEnd = info.usableRoundEnd;
        usableDuringTimescaleSpeeds = info.usableDuringTimescaleSpeeds;
        usableCounterSpeed = info.usableCounterSpeed;
    }

    public void OnNewActionNeeded(bool reset)
    {
        if (!ParentElemental.isAlly)
            return;

        if (reset)
        {
            button.interactable = false;
            return;
        }

        button.interactable = ActionAvailable();
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }

    // Called by Elemental
    public bool ActionAvailable()
    {
        if (ParentElemental.DisengageStrength > 0 && !ParentElemental.mysticArtsActive) // *Mystic Arts
            return false;

        if (OncePerGame && hasOccurredThisGame)
            return false;

        if (OncePerRound && hasOccurredThisRound)
            return false;

        if (ParentElemental.name == "Hellhound") // *Ravenous
        {
            Elemental ally = slotAssignment.GetAlly(ParentElemental);
            if (ally == null || ally.DisengageStrength > 0 || ally.EnrageStrength > 0)
                return false;
        }

        return Clock.CurrentRoundState switch
        {
            Clock.RoundState.RoundStart => usableRoundStart,
            Clock.RoundState.RoundEnd => usableRoundEnd,
            Clock.RoundState.Timescale => usableDuringTimescaleSpeeds,
            Clock.RoundState.Counter => usableCounterSpeed || ParentElemental.mysticArtsActive, // *Mystic Arts
            _ => true //.immediate. What to do here?
        };
    }

    public void TraitBoostInteractable()
    {

    }
}