using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Spell : NetworkBehaviour
{
    //Inherits from NetworkBehaviour to support netcode serialization
    //(this class does not contain networking logic)

    //assigned in prefab:
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text timeScaleText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private bool isCounter;
    private bool isWild;
    private int timeScale;

    private void OnEnable()
    {
        DelegationCore.UpdateInteractable += UpdateInteractable;
    }
    private void OnDisable()
    {
        DelegationCore.UpdateInteractable -= UpdateInteractable;
    }

    //called by Teambuilder (temporarily, will eventually be called by setup)
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

    public void OnClick()
    {

    }

    private void UpdateInteractable(bool turnUninteractable = false)
    {
        //if (turnUninteractable)
        //{
        //    button.interactable = false;
        //    return;
        //}

        //if (Clock.CurrentRoundState == Clock.RoundState.TimeScale)
        //{
        //    if (isWild || Clock.CurrentTimeScale >= timeScale)
        //        button.interactable = true;
        //}
    }
}