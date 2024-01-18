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
        else if (info.timeScale.ToString() == "W")
            isWild = true;
        else
            timeScale = info.timeScale;
    }

    public void OnNewActionNeeded(DelegationCore.DelegationScenario delegationScenario)
    {
        
    }

    public void OnClick()
    {

    }
}