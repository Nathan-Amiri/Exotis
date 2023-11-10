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

    //called by Teambuilder (temporarily, will eventually be called by setup)
    public void Setup(string spellName)
    {
        name = spellName;
        nameText.text = spellName;

        SpellInfo info = Resources.Load<SpellInfo>("SpellInfos/" + spellName);

        Color color = StaticLibrary.gameColors[info.elementColor.ToString()];
        image.color = new Color(color.r/255, color.g/255, color.b/255, 1);

        timeScaleText.text = info.timeScale;
    }

    public void OnClick()
    {

    }
}