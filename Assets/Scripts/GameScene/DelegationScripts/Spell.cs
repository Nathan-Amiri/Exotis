using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Spell : MonoBehaviour, IDelegationAction
{
    // PREFAB REFERENCE:
    [SerializeField] private Elemental parentReference;

    [SerializeField] private Image image;
    [SerializeField] private TMP_Text timescaleText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    // SCENE REFERENCE
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
        // SpellInfo fields
    private bool isCounter;
    public int Timescale { get; private set; } // 0 if counter or wild

    public bool IsWild { get; private set; }
    public bool IsDamaging { get; private set; }
    public bool IsWearying { get; private set; }

    // IDelegationAction fields
    public string ActionType { get; private set; }
    public Elemental ParentElemental { get; private set; }
    public int MaxTargets { get; private set; }
    public bool CanTargetSelf {  get; private set; }
    public bool CanTargetAlly { get; private set; }
    public bool CanTargetEnemy { get; private set; }
    public bool CanTargetBenchedAlly { get; private set; }
    public string Name { get; private set; }

    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    public void Setup(string spellName) 
    {
        // Name in inspector
        name = spellName;
        // IDelegationAction name
        Name = spellName;
        // In-game name
        nameText.text = spellName;

        SpellInfo info = Resources.Load<SpellInfo>("SpellInfos/" + spellName);

        image.color = StaticLibrary.gameColors[info.elementColor.ToString()];

        timescaleText.text = info.timescale.ToString();

        if (info.timescale.ToString() == "C")
            isCounter = true;
        else if (info.timescale.ToString() == "?")
            IsWild = true;
        else
            Timescale = (int)char.GetNumericValue(info.timescale);

        IsDamaging = info.isDamaging;
        IsWearying = info.isWearying;

        ActionType = "spell";
        ParentElemental = parentReference;
        MaxTargets = info.maxTargets;
        CanTargetSelf = info.canTargetSelf;
        CanTargetAlly = info.canTargetAlly;
        CanTargetEnemy = info.canTargetEnemy;
        CanTargetBenchedAlly = info.canTargetBenchedAlly;
    }

    public void OnNewActionNeeded(bool reset = false)
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
        if (ParentElemental.currentActions == 0)
            return false;

        if (ParentElemental.StunStrength > 0)
            return false;

        if (IsWearying && ParentElemental.WearyStrength > 0)
            return false;

        switch (Clock.CurrentRoundState)
        {
            case Clock.RoundState.RoundStart:
                return false;

            case Clock.RoundState.RoundEnd:
                return false;

            case Clock.RoundState.Timescale:
                if (isCounter)
                    return false;

                return IsWild || Clock.CurrentTimescale >= Timescale;

            case Clock.RoundState.Counter:
                return isCounter;

            default: //.immediate. What to do here?
                return true;
        }
    }
}