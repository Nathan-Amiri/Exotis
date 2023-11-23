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
    [SerializeField] private GameObject extra; //everything but display. Set inactive when benched
    [SerializeField] private GameObject status; //flip if enemy
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
        icon.sprite = Resources.Load<Sprite>("ElementalSprites/" + elementalName);

        ElementalInfo info = Resources.Load<ElementalInfo>("ElementalInfos/" + elementalName);

        //call trait setup passing in necessary info

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
        else //if slow
        {
            speedColorBackground.color = StaticLibrary.gameColors["slowHealthBack"];
            healthText.text = "7";
        }

        //check Elemental's y position (if IsOwner, is on the bottom of the screen) and status position
        //to ensure that status is on the correct side
        if (IsOwner != status.transform.localPosition.y > 0)
            status.transform.localPosition *= new Vector2(1, -1);

        //if in slot 4 5 6 or 7, is benched
        if (SlotAssignment.GetSlot(this) > 3)
            ToggleBenched(true);
    }

    public void ToggleBenched(bool benched)
    {
        extra.SetActive(!benched);
    }
}