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
    [SerializeField] private TMP_Text timeScaleText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    // SCENE REFERENCE
    [SerializeField] private DelegationCore delegationCore;

    // DYNAMIC:
        // SpellInfo fields
    private bool isCounter;
    public int TimeScale { get; private set; } // 0 if counter or wild

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

        timeScaleText.text = info.timeScale.ToString();

        if (info.timeScale.ToString() == "C")
            isCounter = true;
        else if (info.timeScale.ToString() == "?")
            IsWild = true;
        else
            TimeScale = (int)char.GetNumericValue(info.timeScale);

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

        if (IsWearying && ParentElemental.isWearied)
            return;

        switch (Clock.CurrentRoundState)
        {
            // If DelegationScenario is RoundStart or RoundEnd, do nothing
            case Clock.RoundState.TimeScale:
                if (isCounter) return;
                button.interactable = IsWild || Clock.CurrentTimeScale >= TimeScale;
                break;
            case Clock.RoundState.Counter:
                button.interactable = isCounter;
                break;
            case Clock.RoundState.Immediate:
                button.interactable = true;
                break;
        }
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}