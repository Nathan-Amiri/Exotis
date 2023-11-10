using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Elemental : NetworkBehaviour
{
    //Inherits from NetworkBehaviour to support netcode serialization
    //(this class does not contain networking logic)

    //assigned in prefab:
    [SerializeField] private List<Image> colorOutlines;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image speedColorBackground;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image icon; //set a to 150 when targeting
    [SerializeField] private GameObject targetButton;

    //called by Teambuilder (temporarily, will eventually be called by setup)
    public void Setup(string elementalName)
    {
        //handle color outline

        name = elementalName;
        nameText.text = elementalName;
        Debug.Log(Resources.Load("ElementalSprites/Dragon"));
        icon.sprite = Resources.Load<Sprite>("ElementalSprites/" + elementalName);

        ElementalInfo info = Resources.Load<ElementalInfo>("ElementalInfos/" + elementalName);

        if (info.speed == ElementalInfo.Speed.fast)
        {
            speedColorBackground.color = StaticLibrary.gameColors["fastHealthBack"];
            healthText.text = "5";
        }
        else if (info.speed == ElementalInfo.Speed.medium)
        {
            speedColorBackground.color = StaticLibrary.gameColors["mediumHealthBack"];
            healthText.text = "6";
        }
        else
        {
            speedColorBackground.color = StaticLibrary.gameColors["slowHealthBack"];
            healthText.text = "7";
        }
    }
}