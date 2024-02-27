using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotButtons : MonoBehaviour
{
    [SerializeField] private List<Button> elementalTargetButtons = new();

    public void TurnOnSlotButtons(List<int> slotButtonNumbers, bool interactable)
    {
        ResetSlotButtons();

        foreach (int slotButtonNumber in slotButtonNumbers)
        {
            elementalTargetButtons[slotButtonNumber].gameObject.SetActive(true);
            elementalTargetButtons[slotButtonNumber].interactable = interactable;
        }
    }

    public void TurnOffSlotButtons(List<int> slotButtonNumbers)
    {
        // Used to turn off certain buttons while leaving others active
        foreach (int slotButtonNumber in slotButtonNumbers)
        {
            elementalTargetButtons[slotButtonNumber].gameObject.SetActive(false);
            elementalTargetButtons[slotButtonNumber].interactable = false;
        }
    }

    public void ResetSlotButtons()
    {
        foreach (Button slotButton in elementalTargetButtons)
        {
            slotButton.gameObject.SetActive(false);
            slotButton.interactable = false;
        }
    }

    public bool AnyTargetsAvailable()
    {
        foreach (Button slotButton in elementalTargetButtons)
            if (slotButton.gameObject.activeSelf)
                return true;

        return false;
    }
}