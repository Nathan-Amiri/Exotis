using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Spell : NetworkBehaviour, IDelegationAction
{
    // Inherits from NetworkBehaviour to support netcode serialization
    // (this class does not contain networking logic)

    // Assigned in prefab:
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
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        switch (delegationScenario)
        {
            case DelegationCore.DelegationScenario.Reset:
                button.interactable = false;
                break;
            case DelegationCore.DelegationScenario.Counter:
                button.interactable = isCounter;
                break;
            case DelegationCore.DelegationScenario.TimeScale:
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