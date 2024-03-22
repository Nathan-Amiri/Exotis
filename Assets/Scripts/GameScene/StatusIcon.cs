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

    // 9 = Mirage
    // 10 = Empower
    // 11 = Numb

    // PREFAB REFERENCE:
    public Image iconImage;
    public TMP_Text initialsText;

        // By status number
    [SerializeField] private List<Sprite> statusSprites = new();

    // CONSTANT:
    private readonly List<(string, Color)> statusStrings = new()
    {
        ("MI", StaticLibrary.gameColors["water"]), // *Mirage
        ("EM", StaticLibrary.gameColors["earth"]), // *Empower
        ("NU", StaticLibrary.gameColors["frost"]), // *Numbing Cold
    };

    public void AddIcon(int statusNumber = 0)
    {
        gameObject.SetActive(true);

        if (statusNumber > 8)
        {
            iconImage.enabled = false;

            (string, Color) initials = statusStrings[statusNumber - 9];
            initialsText.text = initials.Item1;
            initialsText.color = initials.Item2;
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