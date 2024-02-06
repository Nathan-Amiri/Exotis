using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Spell : MonoBehaviour, IDelegationAction
{
    // Assigned in prefab:
    [SerializeField] private Elemental parentElemental;

    [SerializeField] private Image image;
    [SerializeField] private TMP_Text timeScaleText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    // Assigned in scene:
    [SerializeField] private DelegationCore delegationCore;

    // SpellInfo fields
    private bool isCounter;
    private bool isWild;
    private int timeScale;

    // IDelegationAction fields
    public bool IsTargeted { get; private set; }

    private void OnEnable()
    {
        DelegationCore.NewAction += OnNewActionNeeded;
    }
    private void OnDisable()
    {
        DelegationCore.NewAction -= OnNewActionNeeded;
    }

    // Called by Teambuilder (temporarily, will eventually be called by setup)
    public void Setup(string spellName)
    {
        name = spellName;
        nameText.text = spellName;

        SpellInfo info = Resources.Load<SpellInfo>("SpellInfos/" + spellName);

        image.color = StaticLibrary.gameColors[info.elementColor.ToString()];

        timeScaleText.text = info.timeScale.ToString();

        if (info.timeScale.ToString() == "C")
            isCounter = true;
        else if (info.timeScale.ToString() == "?")
            isWild = true;
        else
            timeScale = (int)char.GetNumericValue(info.timeScale);

        IsTargeted = info.isTargeted;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        if (!parentElemental.isAlly)
            return;

        switch (delegationScenario)
        {
            case DelegationCore.DelegationScenario.Reset:
                button.interactable = false;
                break;
            case DelegationCore.DelegationScenario.Counter:
                button.interactable = isCounter;
                break;
            case DelegationCore.DelegationScenario.TimeScale:
                if (isCounter) return;
                button.interactable = isWild || Clock.CurrentTimeScale >= timeScale;
                break;
            // If DelegationScenario is RoundStart or RoundEnd, do nothing
        }
    }

    public void OnClick()
    {
        delegationCore.SelectAction(this);

        // Immediately turn off button so that it cannot be double clicked before the Reset even is invoked
        button.interactable = false;
    }
}