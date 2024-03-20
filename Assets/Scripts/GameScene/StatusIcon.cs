using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusIcon : MonoBehaviour
{
    // Status numbers:
    // 0 = Weary
    // 1 = Enrage
    // 2 = Armor
    // 3 = Disengage
    // 4 = Stun
    // 5 = Slow
    // 6 = Trap
    // 7 = Poison
    // 8 = Weaken
    //.other statuses here

    // PREFAB REFERENCE:
    public Image iconImage;
    public TMP_Text initialsText;

        // By status number
    [SerializeField] private List<Sprite> statusSprites = new();

    // CONSTANT:
    private readonly List<string> statusStrings = new()
    {
        "frenzy"//.example
    };

    public void AddIcon(int statusNumber = 0)
    {
        gameObject.SetActive(true);

        if (statusNumber > 9)
        {
            iconImage.enabled = false;

            initialsText.text = statusStrings[statusNumber - 9];
        }
        else
        {
            iconImage.enabled = true;
            iconImage.sprite = statusSprites[statusNumber];

            initialsText.text = string.Empty;
        }
    }
    public void RemoveIcon()
    {
        gameObject.SetActive(false);
    }
}